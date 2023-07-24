using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace TempleLotViewer.Pages.Common
{
    public class ViewModelBase : LayoutComponentBase
    {
        #region Private Variables

        private bool _isUpdated = true;

        [Inject]
        private IJSRuntime? _jsRuntime { get; set; }

        [Inject]
        private IDialogService? _dialogService { get; set; }

        [Inject]
        private NavigationManager? _navigationManager { get; set; }

        #endregion

        protected override bool ShouldRender()
        {
            if (_isUpdated == false) return false;
            _isUpdated = false;
            return true;

        }

        protected void UpdateProperty<T>(ref T property, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(property, newValue))
            {
                return;
            }

            property = newValue;
            _isUpdated = true;
        }

        protected void UpdateProperty<T>(ref T property, T newValue, Action<T> action)
        {
            if (EqualityComparer<T>.Default.Equals(property, newValue))
            {
                return;
            }

            property = newValue;
            _isUpdated = true;

            action(newValue);
        }

        protected async Task<T?> InvokeAsync<T>(Func<Task<T>> action)
        {
            T? result = default;

            await InvokeAsync(async () =>
            {

                result = await action()
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);

            return result;
        }

        protected Func<Task> CreateEventCallbackAsyncCommand(Func<Task> action, string message)
        {
            return async () =>
            {
                await AttemptActionAsync(async () =>
                {
                    await action()
                        .ConfigureAwait(false);
                }, message)
                .ConfigureAwait(false);

                await RefreshAsync()
                    .ConfigureAwait(false);
            };
        }

        protected Func<T, Task> CreateEventCallbackAsyncCommand<T>(Func<T, Task> action, string message)
        {
            return async (T args) =>
            {
                await AttemptActionAsync(async () =>
                {
                    await action(args)
                        .ConfigureAwait(false);
                }, message)
                .ConfigureAwait(false);

                await RefreshAsync()
                    .ConfigureAwait(false);
            };
        }

        protected async Task AttemptAction(Action action, string message)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (_jsRuntime != null)
                {
                    await _jsRuntime.InvokeAsync<object>("alert", $"{message}: {ex.Message}")
                        .ConfigureAwait(false);
                }
            }

            await RefreshAsync()
                .ConfigureAwait(false);
        }

        protected async Task AttemptActionAsync(Func<Task> actionAsync, string message)
        {
            try
            {
                await actionAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_jsRuntime != null)
                {
                    await _jsRuntime.InvokeAsync<object>("alert", $"{message}: {ex.Message}")
                        .ConfigureAwait(false);
                }
            }

            await RefreshAsync()
                .ConfigureAwait(false);
        }

        protected Task RefreshAsync()
        {
            _isUpdated = true;

            return InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }

        protected static string ConvertBooleanToDisplay(bool value)
        {
            return value
                ? "display: block;"
                : "display: none;";
        }
    }
}
