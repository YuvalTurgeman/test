// Shopping Cart Functions
const ShoppingCart = {
    // Update cart count in navigation
    updateCartCount: function(count) {
        const cartCountElement = document.getElementById('cartCount');
        if (cartCountElement) {
            cartCountElement.textContent = count;
            cartCountElement.style.display = count > 0 ? 'inline' : 'none';
        }
    },

    // Add item to cart
    addToCart: function(bookId, isBorrow) {
        showLoading();

        // Get the antiforgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        fetch('/ShoppingCart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({
                bookId: bookId,
                isBorrow: isBorrow
            })
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                hideLoading();
                if (data.success) {
                    ShoppingCart.updateCartCount(data.cartCount);
                    showMessage('Item added to cart successfully!', 'success');
                } else {
                    showMessage(data.message || 'Failed to add item to cart', 'danger');
                }
            })
            .catch(error => {
                hideLoading();
                console.error('Error:', error);
                showMessage('Failed to add item to cart', 'danger');
            });
    },

    // Remove item from cart
    removeFromCart: function(itemId) {
        if (confirm('Are you sure you want to remove this item from your cart?')) {
            showLoading();
            $.post('/ShoppingCart/RemoveFromCart', { itemId: itemId })
                .done(function(response) {
                    if (response.success) {
                        $(`#cartItem_${itemId}`).fadeOut(300, function() {
                            $(this).remove();
                            ShoppingCart.updateCartCount(response.cartCount);
                            ShoppingCart.updateCartTotal();
                        });
                        showMessage('Item removed from cart.', 'success');
                    }
                })
                .fail(function() {
                    showMessage('Failed to remove item.', 'error');
                })
                .always(function() {
                    hideLoading();
                });
        }
    },

    // Update cart total
    updateCartTotal: function() {
        const cartItems = document.querySelectorAll('.cart-item');
        if (cartItems.length === 0) {
            // If no items, show empty cart message
            $('.cart-items-container').html(
                '<div class="text-center py-5">' +
                '<i class="fas fa-shopping-cart fa-3x text-muted mb-3"></i>' +
                '<h5>Your cart is empty</h5>' +
                '<p class="text-muted">Start adding some amazing books!</p>' +
                '<a href="/Book" class="btn btn-primary">Browse Books</a>' +
                '</div>'
            );
        }
    },

    // Toggle between borrow and buy
    toggleBorrowBuy: function(itemId) {
        showLoading();
        $.post('/ShoppingCart/ToggleBorrowBuy', { itemId: itemId })
            .done(function(response) {
                if (response.success) {
                    // Update price display and badge
                    $(`#price_${itemId}`).text(response.newPrice);
                    $(`#type_${itemId}`).text(response.isBorrow ? 'Borrow' : 'Buy')
                        .removeClass('bg-success bg-info')
                        .addClass(response.isBorrow ? 'bg-info' : 'bg-success');
                    showMessage('Item updated successfully!', 'success');
                }
            })
            .fail(function() {
                showMessage('Failed to update item.', 'error');
            })
            .always(function() {
                hideLoading();
            });
    }
};

// Loading indicator functions
function showLoading() {
    const loadingOverlay = document.querySelector('.loading-overlay');
    if (loadingOverlay) {
        loadingOverlay.classList.add('active');
    }
}

function hideLoading() {
    const loadingOverlay = document.querySelector('.loading-overlay');
    if (loadingOverlay) {
        loadingOverlay.classList.remove('active');
    }
}

// Message display function
function showMessage(message, type = 'info') {
    // Create message element if it doesn't exist
    let messageContainer = document.getElementById('messageContainer');
    if (!messageContainer) {
        messageContainer = document.createElement('div');
        messageContainer.id = 'messageContainer';
        messageContainer.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 1050;';
        document.body.appendChild(messageContainer);
    }

    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    messageContainer.appendChild(alert);

    // Auto-remove after 3 seconds
    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 150);
    }, 3000);
}

// Handle form submissions
document.addEventListener('DOMContentLoaded', function() {
    // Show loading on form submit
    const forms = document.querySelectorAll('form:not(.no-loading)');
    forms.forEach(form => {
        form.addEventListener('submit', function() {
            showLoading();
        });
    });

    // Initialize tooltips
    if (typeof bootstrap !== 'undefined') {
        const tooltips = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        tooltips.forEach(tooltip => new bootstrap.Tooltip(tooltip));
    }

    // Initialize cart count
    if (typeof initialCartCount !== 'undefined') {
        ShoppingCart.updateCartCount(initialCartCount);
    }
});