import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_vi.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
      : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
    delegate,
    GlobalMaterialLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
  ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('vi')
  ];

  /// The application title
  ///
  /// In vi, this message translates to:
  /// **'Homestay'**
  String get appTitle;

  /// No description provided for @home.
  ///
  /// In vi, this message translates to:
  /// **'Trang chủ'**
  String get home;

  /// No description provided for @favorites.
  ///
  /// In vi, this message translates to:
  /// **'Yêu thích'**
  String get favorites;

  /// No description provided for @bookings.
  ///
  /// In vi, this message translates to:
  /// **'Đặt phòng'**
  String get bookings;

  /// No description provided for @profile.
  ///
  /// In vi, this message translates to:
  /// **'Tài khoản'**
  String get profile;

  /// No description provided for @search.
  ///
  /// In vi, this message translates to:
  /// **'Tìm kiếm'**
  String get search;

  /// No description provided for @searchHomestays.
  ///
  /// In vi, this message translates to:
  /// **'Tìm homestay...'**
  String get searchHomestays;

  /// No description provided for @searchResults.
  ///
  /// In vi, this message translates to:
  /// **'Kết quả tìm kiếm'**
  String get searchResults;

  /// No description provided for @login.
  ///
  /// In vi, this message translates to:
  /// **'Đăng nhập'**
  String get login;

  /// No description provided for @register.
  ///
  /// In vi, this message translates to:
  /// **'Đăng ký'**
  String get register;

  /// No description provided for @logout.
  ///
  /// In vi, this message translates to:
  /// **'Đăng xuất'**
  String get logout;

  /// No description provided for @email.
  ///
  /// In vi, this message translates to:
  /// **'Email'**
  String get email;

  /// No description provided for @password.
  ///
  /// In vi, this message translates to:
  /// **'Mật khẩu'**
  String get password;

  /// No description provided for @forgotPassword.
  ///
  /// In vi, this message translates to:
  /// **'Quên mật khẩu?'**
  String get forgotPassword;

  /// No description provided for @dontHaveAccount.
  ///
  /// In vi, this message translates to:
  /// **'Chưa có tài khoản?'**
  String get dontHaveAccount;

  /// No description provided for @alreadyHaveAccount.
  ///
  /// In vi, this message translates to:
  /// **'Đã có tài khoản?'**
  String get alreadyHaveAccount;

  /// No description provided for @price.
  ///
  /// In vi, this message translates to:
  /// **'Giá'**
  String get price;

  /// No description provided for @pricePerNight.
  ///
  /// In vi, this message translates to:
  /// **'đ/đêm'**
  String get pricePerNight;

  /// No description provided for @rating.
  ///
  /// In vi, this message translates to:
  /// **'Đánh giá'**
  String get rating;

  /// No description provided for @reviews.
  ///
  /// In vi, this message translates to:
  /// **'đánh giá'**
  String get reviews;

  /// No description provided for @guests.
  ///
  /// In vi, this message translates to:
  /// **'Khách'**
  String get guests;

  /// No description provided for @bedrooms.
  ///
  /// In vi, this message translates to:
  /// **'Phòng ngủ'**
  String get bedrooms;

  /// No description provided for @bathrooms.
  ///
  /// In vi, this message translates to:
  /// **'Phòng tắm'**
  String get bathrooms;

  /// No description provided for @checkIn.
  ///
  /// In vi, this message translates to:
  /// **'Nhận phòng'**
  String get checkIn;

  /// No description provided for @checkOut.
  ///
  /// In vi, this message translates to:
  /// **'Trả phòng'**
  String get checkOut;

  /// No description provided for @numberOfGuests.
  ///
  /// In vi, this message translates to:
  /// **'Số khách'**
  String get numberOfGuests;

  /// No description provided for @bookNow.
  ///
  /// In vi, this message translates to:
  /// **'Đặt ngay'**
  String get bookNow;

  /// No description provided for @bookingDetails.
  ///
  /// In vi, this message translates to:
  /// **'Chi tiết đặt phòng'**
  String get bookingDetails;

  /// No description provided for @myBookings.
  ///
  /// In vi, this message translates to:
  /// **'Đặt phòng của tôi'**
  String get myBookings;

  /// No description provided for @bookingConfirmed.
  ///
  /// In vi, this message translates to:
  /// **'Đặt phòng thành công'**
  String get bookingConfirmed;

  /// No description provided for @bookingPending.
  ///
  /// In vi, this message translates to:
  /// **'Chờ xác nhận'**
  String get bookingPending;

  /// No description provided for @bookingCancelled.
  ///
  /// In vi, this message translates to:
  /// **'Đã hủy'**
  String get bookingCancelled;

  /// No description provided for @amenities.
  ///
  /// In vi, this message translates to:
  /// **'Tiện nghi'**
  String get amenities;

  /// No description provided for @description.
  ///
  /// In vi, this message translates to:
  /// **'Mô tả'**
  String get description;

  /// No description provided for @location.
  ///
  /// In vi, this message translates to:
  /// **'Địa điểm'**
  String get location;

  /// No description provided for @host.
  ///
  /// In vi, this message translates to:
  /// **'Chủ nhà'**
  String get host;

  /// No description provided for @addToFavorites.
  ///
  /// In vi, this message translates to:
  /// **'Thêm yêu thích'**
  String get addToFavorites;

  /// No description provided for @removeFromFavorites.
  ///
  /// In vi, this message translates to:
  /// **'Xóa yêu thích'**
  String get removeFromFavorites;

  /// No description provided for @share.
  ///
  /// In vi, this message translates to:
  /// **'Chia sẻ'**
  String get share;

  /// No description provided for @compare.
  ///
  /// In vi, this message translates to:
  /// **'So sánh'**
  String get compare;

  /// No description provided for @viewOnMap.
  ///
  /// In vi, this message translates to:
  /// **'Xem trên bản đồ'**
  String get viewOnMap;

  /// No description provided for @directions.
  ///
  /// In vi, this message translates to:
  /// **'Chỉ đường'**
  String get directions;

  /// No description provided for @writeReview.
  ///
  /// In vi, this message translates to:
  /// **'Viết đánh giá'**
  String get writeReview;

  /// No description provided for @submitReview.
  ///
  /// In vi, this message translates to:
  /// **'Gửi đánh giá'**
  String get submitReview;

  /// No description provided for @reviewSubmitted.
  ///
  /// In vi, this message translates to:
  /// **'Đã gửi đánh giá'**
  String get reviewSubmitted;

  /// No description provided for @settings.
  ///
  /// In vi, this message translates to:
  /// **'Cài đặt'**
  String get settings;

  /// No description provided for @language.
  ///
  /// In vi, this message translates to:
  /// **'Ngôn ngữ'**
  String get language;

  /// No description provided for @theme.
  ///
  /// In vi, this message translates to:
  /// **'Giao diện'**
  String get theme;

  /// No description provided for @lightMode.
  ///
  /// In vi, this message translates to:
  /// **'Sáng'**
  String get lightMode;

  /// No description provided for @darkMode.
  ///
  /// In vi, this message translates to:
  /// **'Tối'**
  String get darkMode;

  /// No description provided for @notifications.
  ///
  /// In vi, this message translates to:
  /// **'Thông báo'**
  String get notifications;

  /// No description provided for @messages.
  ///
  /// In vi, this message translates to:
  /// **'Tin nhắn'**
  String get messages;

  /// No description provided for @chat.
  ///
  /// In vi, this message translates to:
  /// **'Trò chuyện'**
  String get chat;

  /// No description provided for @error.
  ///
  /// In vi, this message translates to:
  /// **'Lỗi'**
  String get error;

  /// No description provided for @success.
  ///
  /// In vi, this message translates to:
  /// **'Thành công'**
  String get success;

  /// No description provided for @loading.
  ///
  /// In vi, this message translates to:
  /// **'Đang tải...'**
  String get loading;

  /// No description provided for @noData.
  ///
  /// In vi, this message translates to:
  /// **'Không có dữ liệu'**
  String get noData;

  /// No description provided for @retry.
  ///
  /// In vi, this message translates to:
  /// **'Thử lại'**
  String get retry;

  /// No description provided for @cancel.
  ///
  /// In vi, this message translates to:
  /// **'Hủy'**
  String get cancel;

  /// No description provided for @save.
  ///
  /// In vi, this message translates to:
  /// **'Lưu'**
  String get save;

  /// No description provided for @delete.
  ///
  /// In vi, this message translates to:
  /// **'Xóa'**
  String get delete;

  /// No description provided for @edit.
  ///
  /// In vi, this message translates to:
  /// **'Sửa'**
  String get edit;

  /// No description provided for @add.
  ///
  /// In vi, this message translates to:
  /// **'Thêm'**
  String get add;

  /// No description provided for @apply.
  ///
  /// In vi, this message translates to:
  /// **'Áp dụng'**
  String get apply;

  /// No description provided for @reset.
  ///
  /// In vi, this message translates to:
  /// **'Đặt lại'**
  String get reset;

  /// No description provided for @yes.
  ///
  /// In vi, this message translates to:
  /// **'Có'**
  String get yes;

  /// No description provided for @no.
  ///
  /// In vi, this message translates to:
  /// **'Không'**
  String get no;

  /// No description provided for @ok.
  ///
  /// In vi, this message translates to:
  /// **'OK'**
  String get ok;

  /// No description provided for @confirm.
  ///
  /// In vi, this message translates to:
  /// **'Xác nhận'**
  String get confirm;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'vi'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'vi':
      return AppLocalizationsVi();
  }

  throw FlutterError(
      'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
      'an issue with the localizations generation tool. Please file an issue '
      'on GitHub with a reproducible sample app and the gen-l10n configuration '
      'that was used.');
}
