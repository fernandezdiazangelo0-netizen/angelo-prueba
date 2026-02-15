using SexShopWASM.Models;
using Blazored.LocalStorage;

namespace SexShopWASM.Services;

public interface ICartService
{
    Task<List<CartItem>> GetItems();
    Task AddToCart(Product product);
    Task RemoveFromCart(int productId);
    Task RemoveCompletely(int productId);
    Task ClearCart();
    int GetTotalItems();
    decimal GetTotalPrice();
    event Action OnChange;
}

public class CartService : ICartService
{
    private readonly ILocalStorageService _localStorage;
    private List<CartItem> _items = new();
    private bool _isInitialized = false;

    public CartService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public event Action? OnChange;

    private async Task InitializeIfNeeded()
    {
        if (!_isInitialized)
        {
            try
            {
                var savedItems = await _localStorage.GetItemAsync<List<CartItem>>("cart");
                if (savedItems != null)
                {
                    _items = savedItems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cart from local storage: {ex.Message}");
                _items = new List<CartItem>();
            }
            finally
            {
                _isInitialized = true;
            }
        }
    }

    public async Task<List<CartItem>> GetItems()
    {
        await InitializeIfNeeded();
        return _items ?? new List<CartItem>();
    }

    public async Task AddToCart(Product product)
    {
        try
        {
            await InitializeIfNeeded();
            var item = _items.FirstOrDefault(i => i.Product?.Id == product.Id);
            if (item == null)
            {
                _items.Add(new CartItem { Product = product, Quantity = 1 });
            }
            else
            {
                item.Quantity++;
            }
            await SaveCart();
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding to cart: {ex.Message}");
        }
    }

    public async Task RemoveFromCart(int productId)
    {
        try
        {
            await InitializeIfNeeded();
            var item = _items.FirstOrDefault(i => i.Product?.Id == productId);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    _items.Remove(item);
                }
                await SaveCart();
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing from cart: {ex.Message}");
        }
    }

    public async Task RemoveCompletely(int productId)
    {
        try
        {
            await InitializeIfNeeded();
            var item = _items.FirstOrDefault(i => i.Product?.Id == productId);
            if (item != null)
            {
                _items.Remove(item);
                await SaveCart();
                NotifyStateChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing completely from cart: {ex.Message}");
        }
    }

    public async Task ClearCart()
    {
        try
        {
            _items.Clear();
            await SaveCart();
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing cart: {ex.Message}");
        }
    }

    public int GetTotalItems()
    {
        try
        {
            return _items?.Sum(i => i?.Quantity ?? 0) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public decimal GetTotalPrice()
    {
        try
        {
            return _items?.Sum(i => (i?.Product?.Price ?? 0) * (i?.Quantity ?? 0)) ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task SaveCart()
    {
        try
        {
            if (_items != null)
            {
                await _localStorage.SetItemAsync("cart", _items);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving cart to local storage: {ex.Message}");
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
