import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

import '../../config/api_config.dart';
import '../../config/app_colors.dart';
import '../../models/user.dart';
import '../../services/call_service.dart';
import '../../widgets/user_gradient_background.dart';

// NOTE: This screen contains a minimal WebRTC implementation for peer connection using
// flutter_webrtc. It's a basic POC: no advanced error handling or TURN server configured.

class CallScreen extends StatefulWidget {
  final String callId;
  final CallType callType;
  final User remoteUser;
  final bool isIncoming;

  const CallScreen({
    Key? key,
    required this.callId,
    required this.callType,
    required this.remoteUser,
    this.isIncoming = false,
  }) : super(key: key);

  @override
  State<CallScreen> createState() => _CallScreenState();
}

class _CallScreenState extends State<CallScreen> {
  final CallService _callService = CallService();
  bool _isMuted = false;
  bool _isSpeakerOn = false;
  bool _isVideoOn = true;
  // WebRTC
  RTCPeerConnection? _peerConnection;
  MediaStream? _localStream;
  final RTCVideoRenderer _localRenderer = RTCVideoRenderer();
  final RTCVideoRenderer _remoteRenderer = RTCVideoRenderer();

  // ICE candidates queued before peer is ready
  final List<RTCIceCandidate> _remoteCandidatesQueue = [];

  @override
  void initState() {
    super.initState();
    if (widget.isIncoming) {
      _showIncomingCallDialog();
    }
    _initRenderers();
    _callService.onOfferReceived = _onOfferReceived;
    _callService.onAnswerReceived = _onAnswerReceived;
    _callService.onIceReceived = _onIceReceived;
    // If this screen is opened as the caller, start local media and create an offer
    if (!widget.isIncoming) {
      WidgetsBinding.instance.addPostFrameCallback((_) async {
        await _startLocalMedia();
        await _createPeerConnection();
        try {
          final offer = await _peerConnection?.createOffer();
          if (offer != null) {
            await _peerConnection?.setLocalDescription(offer);
            await _callService.sendOffer(widget.remoteUser.id, widget.callId, {
              'type': offer.type,
              'sdp': offer.sdp,
            });
          }
        } catch (e) {
          // ignore offer/create errors for now
        }
      });
    }
  }

  Future<void> _initRenderers() async {
    await _localRenderer.initialize();
    await _remoteRenderer.initialize();
  }

  @override
  void dispose() {
    // Unregister callbacks
    _callService.onOfferReceived = null;
    _callService.onAnswerReceived = null;
    _callService.onIceReceived = null;
    // Close/cleanup peer and media
    _closePeerConnection();
    super.dispose();
  }

  String _getInitial(User user) {
    final full = (user.fullName ?? '').trim();
    final name = full.isNotEmpty ? full : (user.userName.trim());
    return name.isNotEmpty ? name[0].toUpperCase() : '?';
  }

  void _showIncomingCallDialog() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => AlertDialog(
          backgroundColor: Colors.transparent,
          elevation: 0,
          content: Container(
            padding: const EdgeInsets.all(20),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(20),
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircleAvatar(
                  radius: 50,
                  backgroundImage: widget.remoteUser.avatarUrl != null
                      ? CachedNetworkImageProvider(widget.remoteUser.avatarUrl!)
                      : null,
                  child: widget.remoteUser.avatarUrl == null
                      ? Text(
                          _getInitial(widget.remoteUser),
                          style: const TextStyle(fontSize: 30, color: Colors.white),
                        )
                      : null,
                ),
                const SizedBox(height: 20),
                Text(
                  widget.remoteUser.fullName ?? widget.remoteUser.userName,
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  widget.callType == CallType.audio ? 'Cuộc gọi thoại' : 'Cuộc gọi video',
                  style: TextStyle(
                    fontSize: 16,
                    color: Colors.grey[600],
                  ),
                ),
                const SizedBox(height: 30),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  children: [
                    ElevatedButton(
                      onPressed: () {
                        Navigator.of(context).pop();
                        _rejectCall();
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.red,
                        shape: const CircleBorder(),
                        padding: const EdgeInsets.all(20),
                      ),
                      child: const Icon(
                        Icons.call_end,
                        color: Colors.white,
                        size: 30,
                      ),
                    ),
                    ElevatedButton(
                      onPressed: () {
                        Navigator.of(context).pop();
                        _acceptCall();
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.green,
                        shape: const CircleBorder(),
                        padding: const EdgeInsets.all(20),
                      ),
                      child: const Icon(
                        Icons.call,
                        color: Colors.white,
                        size: 30,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      );
    });
  }

  Future<void> _acceptCall() async {
    final result = await _callService.acceptCall(widget.callId);
    if (result['success'] == true) {
      // Call accepted — start WebRTC negotiation as callee
      await _startLocalMedia();
      await _createPeerConnection();
      // callee waits for remote offer from hub; when offer received handler runs, it will create answer
    } else {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(result['message'] ?? 'Không thể chấp nhận cuộc gọi')),
        );
        Navigator.of(context).pop();
      }
    }
  }

  Future<void> _rejectCall() async {
    await _callService.rejectCall(widget.callId);
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  Future<void> _endCall() async {
    await _callService.endCall(widget.callId);
    await _closePeerConnection();
    if (mounted) {
      Navigator.of(context).pop();
    }
  }

  Future<void> _startLocalMedia() async {
    try {
      final constraints = <String, dynamic>{
        'audio': true,
        'video': widget.callType == CallType.video
            ? {
                'facingMode': 'user',
              }
            : false,
      };

      final stream = await navigator.mediaDevices.getUserMedia(constraints);
      _localStream = stream;
      _localRenderer.srcObject = _localStream;

      // Add tracks to peer if already created
      if (_peerConnection != null) {
        for (var track in _localStream!.getTracks()) {
          _peerConnection!.addTrack(track, _localStream!);
        }
      }
    } catch (e) {
      // ignore
    }
  }

  Future<void> _createPeerConnection() async {
    final config = <String, dynamic>{
      'iceServers': ApiConfig.rtcIceServers,
    };

    final pc = await createPeerConnection(config);
    _peerConnection = pc;

    // Add local tracks if available
    if (_localStream != null) {
      for (var track in _localStream!.getTracks()) {
        _peerConnection!.addTrack(track, _localStream!);
      }
    }

    pc.onIceCandidate = (candidate) {
      // candidate is non-nullable in this callback; send directly
      _callService.sendIce(widget.remoteUser.id, widget.callId, {
        'candidate': candidate.candidate,
        'sdpMid': candidate.sdpMid,
        'sdpMLineIndex': candidate.sdpMLineIndex,
      });
    };

    pc.onTrack = (event) {
      if (event.streams.isNotEmpty) {
        _remoteRenderer.srcObject = event.streams[0];
      }
    };

    // Drain queued remote candidates
    for (final c in _remoteCandidatesQueue) {
      pc.addCandidate(c);
    }
    _remoteCandidatesQueue.clear();
  }

  Future<void> _closePeerConnection() async {
    try {
      await _localRenderer.dispose();
      await _remoteRenderer.dispose();
      await _localStream?.dispose();
      await _peerConnection?.close();
      _peerConnection = null;
    } catch (e) {}
  }

  // Handlers for signaling messages from CallService
  void _onOfferReceived(String callId, Map<String, dynamic> offer) async {
    if (callId != widget.callId) return;
    await _startLocalMedia();
    await _createPeerConnection();
    try {
      await _peerConnection?.setRemoteDescription(RTCSessionDescription(offer['sdp'], offer['type']));
      final answer = await _peerConnection?.createAnswer();
      if (answer != null) {
        await _peerConnection?.setLocalDescription(answer);
        await _callService.sendAnswer(widget.remoteUser.id, widget.callId, {
          'type': answer.type,
          'sdp': answer.sdp,
        });
      }
    } catch (e) {}
  }

  void _onAnswerReceived(String callId, Map<String, dynamic> answer) async {
    if (callId != widget.callId) return;
    try {
      await _peerConnection?.setRemoteDescription(RTCSessionDescription(answer['sdp'], answer['type']));
    } catch (e) {}
  }

  void _onIceReceived(String callId, Map<String, dynamic> candidate) async {
    if (callId != widget.callId) return;
    try {
      final c = RTCIceCandidate(candidate['candidate'], candidate['sdpMid'], candidate['sdpMLineIndex']);
      if (_peerConnection != null) {
        await _peerConnection!.addCandidate(c);
      } else {
        _remoteCandidatesQueue.add(c);
      }
    } catch (e) {
      // ignore parse/add errors for now
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: UserGradientBackground(
        child: Stack(
          children: [
          // Background - for video call show remote video stream if available
          if (widget.callType == CallType.video)
            Container(
              color: Colors.black,
              child: _remoteRenderer.srcObject != null
                  ? RTCVideoView(_remoteRenderer, objectFit: RTCVideoViewObjectFit.RTCVideoViewObjectFitCover)
                  : Center(
                      child: Icon(
                        Icons.videocam_off,
                        size: 100,
                        color: Colors.white54,
                      ),
                    ),
            )
          else
            Container(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    AppColors.primary,
                    AppColors.primary.withOpacity(0.8),
                  ],
                ),
              ),
            ),

          // Call info overlay
          SafeArea(
            child: Column(
              children: [
                // Header
                Padding(
                  padding: const EdgeInsets.all(20),
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 30,
                        backgroundImage: widget.remoteUser.avatarUrl != null
                            ? CachedNetworkImageProvider(widget.remoteUser.avatarUrl!)
                            : null,
                        child: widget.remoteUser.avatarUrl == null
                            ? Text(
                                _getInitial(widget.remoteUser),
                                style: const TextStyle(fontSize: 20, color: Colors.white),
                              )
                            : null,
                      ),
                      const SizedBox(width: 15),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              widget.remoteUser.fullName ?? widget.remoteUser.userName,
                              style: const TextStyle(
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                                color: Colors.white,
                              ),
                            ),
                            Text(
                              'Đang gọi...',
                              style: TextStyle(
                                fontSize: 16,
                                color: Colors.white.withOpacity(0.8),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),

                const Spacer(),

                // Call duration (placeholder)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                  decoration: BoxDecoration(
                    color: Colors.black.withOpacity(0.3),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: const Text(
                    '00:00',
                    style: TextStyle(
                      fontSize: 18,
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),

                const Spacer(),

                // Self video preview (for video calls)
                if (widget.callType == CallType.video)
                  Align(
                    alignment: Alignment.topRight,
                    child: Padding(
                      padding: const EdgeInsets.only(top: 100.0, right: 20.0),
                      child: Container(
                        width: 120,
                        height: 160,
                        decoration: BoxDecoration(
                          color: Colors.grey[800],
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: Colors.white, width: 2),
                        ),
                        child: _localRenderer.srcObject != null
                            ? RTCVideoView(_localRenderer, mirror: true, objectFit: RTCVideoViewObjectFit.RTCVideoViewObjectFitCover)
                            : const Center(
                                child: Icon(
                                  Icons.videocam_off,
                                  color: Colors.white54,
                                  size: 40,
                                ),
                              ),
                      ),
                    ),
                  ),

                // Control buttons
                Container(
                  padding: const EdgeInsets.all(20),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                    children: [
                      // Mute button
                      _buildControlButton(
                        icon: _isMuted ? Icons.mic_off : Icons.mic,
                        label: _isMuted ? 'Bật mic' : 'Tắt mic',
                        onPressed: () {
                          setState(() => _isMuted = !_isMuted);
                        },
                        backgroundColor: _isMuted ? Colors.red : Colors.white24,
                      ),

                      // Speaker button (audio call only)
                      if (widget.callType == CallType.audio)
                        _buildControlButton(
                          icon: _isSpeakerOn ? Icons.volume_up : Icons.volume_off,
                          label: _isSpeakerOn ? 'Loa ngoài' : 'Loa trong',
                          onPressed: () {
                            setState(() => _isSpeakerOn = !_isSpeakerOn);
                          },
                          backgroundColor: Colors.white24,
                        ),

                      // Video toggle (video call only)
                      if (widget.callType == CallType.video)
                        _buildControlButton(
                          icon: _isVideoOn ? Icons.videocam : Icons.videocam_off,
                          label: _isVideoOn ? 'Tắt camera' : 'Bật camera',
                          onPressed: () {
                            setState(() => _isVideoOn = !_isVideoOn);
                          },
                          backgroundColor: _isVideoOn ? Colors.white24 : Colors.red,
                        ),

                      // End call button
                      _buildControlButton(
                        icon: Icons.call_end,
                        label: 'Kết thúc',
                        onPressed: _endCall,
                        backgroundColor: Colors.red,
                        iconColor: Colors.white,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
        ),
      ),
    );
  }

  Widget _buildControlButton({
    required IconData icon,
    required String label,
    required VoidCallback onPressed,
    required Color backgroundColor,
    Color iconColor = Colors.white,
  }) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        ElevatedButton(
          onPressed: onPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: backgroundColor,
            shape: const CircleBorder(),
            padding: const EdgeInsets.all(16),
            elevation: 0,
          ),
          child: Icon(
            icon,
            color: iconColor,
            size: 28,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          label,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 12,
          ),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}