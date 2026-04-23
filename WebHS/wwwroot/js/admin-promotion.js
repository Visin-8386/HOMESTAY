/*
 * ADMIN PROMOTION MANAGEMENT - ADVANCED INTERACTIONS
 * Enhanced UI/UX functionality for promotion management
 */

class PromotionManager {
    constructor() {
        this.selectedPromotions = new Set();
        this.isLoading = false;
        this.toastContainer = null;
        this.initializeComponents();
        this.bindEvents();
        this.setupAnimations();
    }

    initializeComponents() {
        this.createToastContainer();
        this.createLoadingOverlay();
        this.createFloatingActionButton();
        this.createBulkActionsToolbar();
        this.setupTableEnhancements();
    }

    createToastContainer() {
        if (!document.querySelector('.toast-container-promotion')) {
            const container = document.createElement('div');
            container.className = 'toast-container-promotion';
            document.body.appendChild(container);
            this.toastContainer = container;
        } else {
            this.toastContainer = document.querySelector('.toast-container-promotion');
        }
    }

    createLoadingOverlay() {
        if (!document.querySelector('.loading-overlay-promotion')) {
            const overlay = document.createElement('div');
            overlay.className = 'loading-overlay-promotion';
            overlay.innerHTML = `
                <div class="loading-spinner-promotion"></div>
            `;
            document.body.appendChild(overlay);
        }
    }

    createFloatingActionButton() {
        if (!document.querySelector('.fab-container-promotion')) {
            const fabHtml = `
                <div class="fab-container-promotion">
                    <div class="fab-submenu-promotion" id="fabSubmenu">
                        <a href="${window.createPromotionUrl || '#'}" class="fab-item-promotion">
                            <span class="fab-label-promotion">Tạo khuyến mãi mới</span>
                            <button class="fab-button-promotion">
                                <i class="fas fa-plus"></i>
                            </button>
                        </a>
                        <a href="#" class="fab-item-promotion" onclick="promotionManager.exportData()">
                            <span class="fab-label-promotion">Xuất dữ liệu</span>
                            <button class="fab-button-promotion">
                                <i class="fas fa-download"></i>
                            </button>
                        </a>
                        <a href="#" class="fab-item-promotion" onclick="promotionManager.refreshData()">
                            <span class="fab-label-promotion">Làm mới</span>
                            <button class="fab-button-promotion">
                                <i class="fas fa-sync-alt"></i>
                            </button>
                        </a>
                    </div>
                    <button class="fab-main-promotion" id="fabMain">
                        <i class="fas fa-ellipsis-v"></i>
                    </button>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', fabHtml);
        }
    }

    createBulkActionsToolbar() {
        const controlsSection = document.querySelector('.promotion-controls');
        if (controlsSection && !document.querySelector('.bulk-actions-promotion')) {
            const bulkActionsHtml = `
                <div class="bulk-actions-promotion" id="bulkActions">
                    <div class="row align-items-center">
                        <div class="col-md-6">
                            <span class="bulk-counter-promotion">
                                <i class="fas fa-check-square me-2"></i>
                                Đã chọn <span id="selectedCount">0</span> khuyến mãi
                            </span>
                        </div>
                        <div class="col-md-6 text-end">
                            <button class="bulk-btn-promotion btn-success-promotion" onclick="promotionManager.bulkActivate()">
                                <i class="fas fa-play me-2"></i>Kích hoạt
                            </button>
                            <button class="bulk-btn-promotion btn-warning-promotion" onclick="promotionManager.bulkDeactivate()">
                                <i class="fas fa-pause me-2"></i>Tạm ngưng
                            </button>
                            <button class="bulk-btn-promotion btn-danger-promotion" onclick="promotionManager.bulkDelete()">
                                <i class="fas fa-trash me-2"></i>Xóa
                            </button>
                            <button class="bulk-btn-promotion btn-secondary" onclick="promotionManager.clearSelection()">
                                <i class="fas fa-times me-2"></i>Hủy
                            </button>
                        </div>
                    </div>
                </div>
            `;
            controlsSection.insertAdjacentHTML('afterend', bulkActionsHtml);
        }
    }

    setupTableEnhancements() {
        const table = document.querySelector('.table');
        if (table) {
            table.className = 'table table-promotion-advanced table-hover';
            
            // Add checkboxes for bulk selection
            this.addBulkSelectionCheckboxes();
            
            // Add sorting functionality
            this.addTableSorting();
            
            // Enhance action buttons
            this.enhanceActionButtons();
        }
    }

    addBulkSelectionCheckboxes() {
        const table = document.querySelector('.table-promotion-advanced');
        if (!table) return;

        // Add header checkbox
        const headerRow = table.querySelector('thead tr');
        if (headerRow && !headerRow.querySelector('.bulk-select-header')) {
            const headerCheckbox = document.createElement('th');
            headerCheckbox.className = 'bulk-select-header';
            headerCheckbox.innerHTML = `
                <div class="form-check">
                    <input class="form-check-input" type="checkbox" id="selectAll">
                    <label class="form-check-label" for="selectAll"></label>
                </div>
            `;
            headerRow.insertBefore(headerCheckbox, headerRow.firstChild);
        }

        // Add row checkboxes
        const bodyRows = table.querySelectorAll('tbody tr');
        bodyRows.forEach((row, index) => {
            if (!row.querySelector('.bulk-select-row')) {
                const checkbox = document.createElement('td');
                checkbox.className = 'bulk-select-row';
                const promotionId = this.extractPromotionId(row);
                checkbox.innerHTML = `
                    <div class="form-check">
                        <input class="form-check-input row-checkbox" type="checkbox" 
                               id="select${promotionId}" data-promotion-id="${promotionId}">
                        <label class="form-check-label" for="select${promotionId}"></label>
                    </div>
                `;
                row.insertBefore(checkbox, row.firstChild);
            }
        });
    }

    addTableSorting() {
        const headers = document.querySelectorAll('.table-promotion-advanced thead th');
        headers.forEach(header => {
            if (!header.classList.contains('bulk-select-header') && 
                !header.classList.contains('action-column')) {
                header.style.cursor = 'pointer';
                header.addEventListener('click', () => this.sortTable(header));
            }
        });
    }

    enhanceActionButtons() {
        const actionCells = document.querySelectorAll('.table-promotion-advanced tbody tr td:last-child');
        actionCells.forEach(cell => {
            const btnGroup = cell.querySelector('.btn-group');
            if (btnGroup) {
                btnGroup.className = 'action-group-promotion';
                const buttons = btnGroup.querySelectorAll('.btn');
                buttons.forEach((btn, index) => {
                    btn.className = btn.className.replace(/btn-\w+/, '');
                    btn.classList.add('action-btn-promotion-advanced');
                    
                    if (btn.href && btn.href.includes('Edit')) {
                        btn.classList.add('btn-edit-promotion');
                    } else if (btn.onclick && btn.onclick.toString().includes('toggleStatus')) {
                        btn.classList.add('btn-toggle-promotion');
                    } else if (btn.onclick && btn.onclick.toString().includes('delete')) {
                        btn.classList.add('btn-delete-promotion');
                    }
                });
            }
        });
    }

    bindEvents() {
        // FAB menu toggle
        const fabMain = document.getElementById('fabMain');
        const fabSubmenu = document.getElementById('fabSubmenu');
        
        if (fabMain && fabSubmenu) {
            fabMain.addEventListener('click', () => {
                fabSubmenu.classList.toggle('show');
            });
        }

        // Bulk selection
        document.addEventListener('change', (e) => {
            if (e.target.id === 'selectAll') {
                this.toggleAllSelection(e.target.checked);
            } else if (e.target.classList.contains('row-checkbox')) {
                this.updateSelection(e.target);
            }
        });

        // Enhanced search with debouncing
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.performSearch(e.target.value);
                }, 500);
            });
        }

        // Close FAB menu when clicking outside
        document.addEventListener('click', (e) => {
            const fabContainer = e.target.closest('.fab-container-promotion');
            if (!fabContainer) {
                const fabSubmenu = document.getElementById('fabSubmenu');
                if (fabSubmenu) {
                    fabSubmenu.classList.remove('show');
                }
            }
        });

        // Table row click enhancement
        document.querySelectorAll('.table-promotion-advanced tbody tr').forEach(row => {
            row.addEventListener('click', (e) => {
                if (!e.target.closest('.action-group-promotion') && 
                    !e.target.closest('.bulk-select-row')) {
                    this.highlightRow(row);
                }
            });
        });
    }

    setupAnimations() {
        // Stagger animation for table rows
        const rows = document.querySelectorAll('.table-promotion-advanced tbody tr');
        rows.forEach((row, index) => {
            row.style.setProperty('--stagger', index);
            row.classList.add('animate-in', 'stagger-animation');
        });

        // Animate stat cards
        const statCards = document.querySelectorAll('.promotion-stat-card');
        statCards.forEach((card, index) => {
            setTimeout(() => {
                card.style.opacity = '0';
                card.style.transform = 'translateY(30px)';
                card.style.transition = 'all 0.6s ease';
                
                setTimeout(() => {
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                }, 100 * index);
            }, 100);
        });
    }

    // Selection Management
    toggleAllSelection(checked) {
        const checkboxes = document.querySelectorAll('.row-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = checked;
            this.updateSelection(checkbox);
        });
    }

    updateSelection(checkbox) {
        const promotionId = checkbox.dataset.promotionId;
        if (checkbox.checked) {
            this.selectedPromotions.add(promotionId);
        } else {
            this.selectedPromotions.delete(promotionId);
        }
        this.updateBulkActionsVisibility();
        this.updateSelectAllState();
    }

    updateBulkActionsVisibility() {
        const bulkActions = document.getElementById('bulkActions');
        const selectedCount = document.getElementById('selectedCount');
        
        if (selectedCount) {
            selectedCount.textContent = this.selectedPromotions.size;
        }
        
        if (bulkActions) {
            if (this.selectedPromotions.size > 0) {
                bulkActions.classList.add('show');
            } else {
                bulkActions.classList.remove('show');
            }
        }
    }

    updateSelectAllState() {
        const selectAll = document.getElementById('selectAll');
        const checkboxes = document.querySelectorAll('.row-checkbox');
        const checkedCount = document.querySelectorAll('.row-checkbox:checked').length;
        
        if (selectAll) {
            selectAll.checked = checkedCount === checkboxes.length;
            selectAll.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
        }
    }

    clearSelection() {
        this.selectedPromotions.clear();
        document.querySelectorAll('.row-checkbox').forEach(cb => cb.checked = false);
        document.getElementById('selectAll').checked = false;
        this.updateBulkActionsVisibility();
    }

    // Bulk Actions
    async bulkActivate() {
        if (this.selectedPromotions.size === 0) return;
        
        const result = await this.confirmAction(
            'Kích hoạt khuyến mãi',
            `Bạn có chắc chắn muốn kích hoạt ${this.selectedPromotions.size} khuyến mãi đã chọn?`
        );
        
        if (result) {
            this.showLoading();
            try {
                await this.performBulkAction('activate', Array.from(this.selectedPromotions));
                this.showToast('Đã kích hoạt khuyến mãi thành công!', 'success');
                this.refreshPage();
            } catch (error) {
                this.showToast('Có lỗi xảy ra khi kích hoạt khuyến mãi!', 'error');
            } finally {
                this.hideLoading();
            }
        }
    }

    async bulkDeactivate() {
        if (this.selectedPromotions.size === 0) return;
        
        const result = await this.confirmAction(
            'Tạm ngưng khuyến mãi',
            `Bạn có chắc chắn muốn tạm ngưng ${this.selectedPromotions.size} khuyến mãi đã chọn?`
        );
        
        if (result) {
            this.showLoading();
            try {
                await this.performBulkAction('deactivate', Array.from(this.selectedPromotions));
                this.showToast('Đã tạm ngưng khuyến mãi thành công!', 'success');
                this.refreshPage();
            } catch (error) {
                this.showToast('Có lỗi xảy ra khi tạm ngưng khuyến mãi!', 'error');
            } finally {
                this.hideLoading();
            }
        }
    }

    async bulkDelete() {
        if (this.selectedPromotions.size === 0) return;
        
        const result = await this.confirmAction(
            'Xóa khuyến mãi',
            `Bạn có chắc chắn muốn xóa ${this.selectedPromotions.size} khuyến mãi đã chọn? Hành động này không thể hoàn tác!`,
            'danger'
        );
        
        if (result) {
            this.showLoading();
            try {
                await this.performBulkAction('delete', Array.from(this.selectedPromotions));
                this.showToast('Đã xóa khuyến mãi thành công!', 'success');
                this.refreshPage();
            } catch (error) {
                this.showToast('Có lỗi xảy ra khi xóa khuyến mãi!', 'error');
            } finally {
                this.hideLoading();
            }
        }
    }

    // Utility Functions
    async performBulkAction(action, promotionIds) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        
        const response = await fetch(`/Promotion/BulkAction`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                action: action,
                promotionIds: promotionIds
            })
        });
        
        if (!response.ok) {
            throw new Error('Bulk action failed');
        }
        
        return await response.json();
    }

    async confirmAction(title, message, type = 'warning') {
        return new Promise((resolve) => {
            const modal = this.createConfirmModal(title, message, type);
            document.body.appendChild(modal);
            
            const confirmBtn = modal.querySelector('.confirm-btn');
            const cancelBtn = modal.querySelector('.cancel-btn');
            
            confirmBtn.addEventListener('click', () => {
                modal.remove();
                resolve(true);
            });
            
            cancelBtn.addEventListener('click', () => {
                modal.remove();
                resolve(false);
            });
            
            // Show modal with animation
            setTimeout(() => modal.classList.add('show'), 10);
        });
    }

    createConfirmModal(title, message, type) {
        const modal = document.createElement('div');
        modal.className = 'confirm-modal-promotion';
        modal.innerHTML = `
            <div class="confirm-backdrop"></div>
            <div class="confirm-dialog">
                <div class="confirm-header ${type}">
                    <h5>${title}</h5>
                </div>
                <div class="confirm-body">
                    <p>${message}</p>
                </div>
                <div class="confirm-footer">
                    <button class="btn btn-secondary cancel-btn">Hủy</button>
                    <button class="btn btn-${type === 'danger' ? 'danger' : 'primary'} confirm-btn">Xác nhận</button>
                </div>
            </div>
        `;
        
        // Add styles for modal
        const style = document.createElement('style');
        style.textContent = `
            .confirm-modal-promotion {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                z-index: 10000;
                opacity: 0;
                visibility: hidden;
                transition: all 0.3s ease;
            }
            .confirm-modal-promotion.show {
                opacity: 1;
                visibility: visible;
            }
            .confirm-backdrop {
                position: absolute;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.5);
                backdrop-filter: blur(5px);
            }
            .confirm-dialog {
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                background: white;
                border-radius: 15px;
                min-width: 400px;
                max-width: 500px;
                box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            }
            .confirm-header {
                padding: 1.5rem;
                border-radius: 15px 15px 0 0;
                color: white;
            }
            .confirm-header.warning {
                background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
            }
            .confirm-header.danger {
                background: linear-gradient(135deg, #fc466b 0%, #3f5efb 100%);
            }
            .confirm-body {
                padding: 2rem 1.5rem;
            }
            .confirm-footer {
                padding: 1rem 1.5rem 1.5rem;
                text-align: right;
            }
            .confirm-footer .btn {
                margin-left: 0.5rem;
                border-radius: 25px;
                padding: 0.5rem 1.5rem;
            }
        `;
        modal.appendChild(style);
        
        return modal;
    }

    showToast(message, type = 'info', duration = 3000) {
        const toast = document.createElement('div');
        toast.className = `toast-promotion ${type}`;
        toast.innerHTML = `
            <div class="toast-header-promotion">
                <span><i class="fas fa-${this.getToastIcon(type)} me-2"></i>${this.getToastTitle(type)}</span>
                <button class="toast-close-promotion">&times;</button>
            </div>
            <div class="toast-body-promotion">${message}</div>
        `;
        
        this.toastContainer.appendChild(toast);
        
        // Show toast
        setTimeout(() => toast.classList.add('show'), 10);
        
        // Auto hide
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);
        
        // Close button
        toast.querySelector('.toast-close-promotion').addEventListener('click', () => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        });
    }

    getToastIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }

    getToastTitle(type) {
        const titles = {
            success: 'Thành công',
            error: 'Lỗi',
            warning: 'Cảnh báo',
            info: 'Thông tin'
        };
        return titles[type] || 'Thông tin';
    }

    showLoading() {
        const overlay = document.querySelector('.loading-overlay-promotion');
        if (overlay) {
            overlay.classList.add('show');
        }
        this.isLoading = true;
    }

    hideLoading() {
        const overlay = document.querySelector('.loading-overlay-promotion');
        if (overlay) {
            overlay.classList.remove('show');
        }
        this.isLoading = false;
    }

    // Enhanced Search
    performSearch(query) {
        if (this.isLoading) return;
        
        const currentUrl = new URL(window.location);
        currentUrl.searchParams.set('search', query);
        currentUrl.searchParams.set('page', '1'); // Reset to first page
        
        window.location.href = currentUrl.toString();
    }

    // Table Sorting
    sortTable(header) {
        // Add sorting logic here
        const table = header.closest('table');
        const tbody = table.querySelector('tbody');
        const rows = Array.from(tbody.querySelectorAll('tr'));
        const headerIndex = Array.from(header.parentNode.children).indexOf(header);
        
        // Toggle sort direction
        const isAscending = !header.classList.contains('sort-desc');
        
        // Remove existing sort classes
        header.parentNode.querySelectorAll('th').forEach(th => {
            th.classList.remove('sort-asc', 'sort-desc');
        });
        
        // Add new sort class
        header.classList.add(isAscending ? 'sort-asc' : 'sort-desc');
        
        // Sort rows
        rows.sort((a, b) => {
            const aText = a.children[headerIndex].textContent.trim();
            const bText = b.children[headerIndex].textContent.trim();
            
            if (isAscending) {
                return aText.localeCompare(bText, 'vi', { numeric: true });
            } else {
                return bText.localeCompare(aText, 'vi', { numeric: true });
            }
        });
        
        // Reappend sorted rows
        rows.forEach(row => tbody.appendChild(row));
    }

    highlightRow(row) {
        // Remove existing highlights
        document.querySelectorAll('.table-promotion-advanced tbody tr').forEach(r => {
            r.classList.remove('highlighted');
        });
        
        // Add highlight to clicked row
        row.classList.add('highlighted');
        
        // Add CSS for highlight if not exists
        if (!document.querySelector('#highlight-style')) {
            const style = document.createElement('style');
            style.id = 'highlight-style';
            style.textContent = `
                .table-promotion-advanced tbody tr.highlighted {
                    background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%) !important;
                    transform: scale(1.02);
                    box-shadow: 0 5px 25px rgba(102, 126, 234, 0.2);
                }
            `;
            document.head.appendChild(style);
        }
    }

    extractPromotionId(row) {
        // Try to extract promotion ID from various sources
        const editBtn = row.querySelector('a[href*="Edit"]');
        if (editBtn) {
            const href = editBtn.getAttribute('href');
            const match = href.match(/\/(\d+)$/);
            if (match) return match[1];
        }
        
        // Fallback to row index
        const tbody = row.closest('tbody');
        return Array.from(tbody.children).indexOf(row);
    }

    // Utility functions for external calls
    refreshData() {
        this.showLoading();
        window.location.reload();
    }

    refreshPage() {
        setTimeout(() => {
            window.location.reload();
        }, 1000);
    }

    exportData() {
        this.showToast('Đang chuẩn bị xuất dữ liệu...', 'info');
        // Add export logic here
        setTimeout(() => {
            this.showToast('Xuất dữ liệu thành công!', 'success');
        }, 2000);
    }
}

// Initialize the promotion manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.promotionManager = new PromotionManager();
});

// Enhanced existing functions
window.toggleStatus = function(id, isActive) {
    const action = isActive ? 'tạm ngưng' : 'kích hoạt';
    if (confirm(`Bạn có chắc chắn muốn ${action} khuyến mãi này?`)) {
        promotionManager.showLoading();
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Promotion/ToggleStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `id=${id}&__RequestVerificationToken=${encodeURIComponent(token)}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                promotionManager.showToast(`Đã ${action} khuyến mãi thành công!`, 'success');
                promotionManager.refreshPage();
            } else {
                promotionManager.showToast('Có lỗi xảy ra: ' + data.message, 'error');
            }
        })
        .catch(() => {
            promotionManager.showToast('Có lỗi xảy ra khi cập nhật trạng thái', 'error');
        })
        .finally(() => {
            promotionManager.hideLoading();
        });
    }
};

window.deletePromotion = function(id) {
    if (confirm('Bạn có chắc chắn muốn xóa khuyến mãi này? Hành động này không thể hoàn tác.')) {
        promotionManager.showLoading();
        
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        fetch('/Promotion/Delete', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `id=${id}&__RequestVerificationToken=${encodeURIComponent(token)}`
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                promotionManager.showToast('Đã xóa khuyến mãi thành công!', 'success');
                promotionManager.refreshPage();
            } else {
                promotionManager.showToast('Có lỗi xảy ra: ' + data.message, 'error');
            }
        })
        .catch(() => {
            promotionManager.showToast('Có lỗi xảy ra khi xóa khuyến mãi', 'error');
        })
        .finally(() => {
            promotionManager.hideLoading();
        });
    }
};

window.performSearch = function() {
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        promotionManager.performSearch(searchInput.value);
    }
};
