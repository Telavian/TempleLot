using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Text;
using System.Web;
using TempleLotViewer.Extensions;
using TempleLotViewer.Pages.Common;
using TempleLotViewer.Pages.Models;
using TempleLotViewer.Search;
using TempleLotViewer.Services.FileService.Interfaces;
using TempleLotViewer.Services.WitnessSearch;
using TempleLotViewer.Services.WitnessSearch.Models;

namespace TempleLotViewer.Pages
{
    public class DisplayPageBase : ViewModelBase
    {
        #region Private Variables

        [Inject]
        private NavigationManager? _navManager { get; set; }

        [Inject]
        private IJSRuntime? _jsRuntime { get; set; }

        [Inject]
        private IDialogService? _dialogService { get; set; }

        [Inject]
        private IFileService? _fileService { get; set; }

        private TaskCompletionSource _browserInitialization = new TaskCompletionSource();

        #endregion

        #region Publlc Properties

        public static WitnessSearchService? WitnessSearch { get; set; }
        public WitnessContent[] AllWitnesses { get; set; } = Array.Empty<WitnessContent>();
        public WitnessContent[] FilteredWitnesses { get; set; } = Array.Empty<WitnessContent>();

        #region SearchText

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set => UpdateProperty(ref _searchText, value);
        }

        #endregion

        #region WitnessFilterText
        private string _witnessFilterText = "";
        public string WitnessFilterText
        {
            get => _witnessFilterText;
            set => UpdateProperty(ref _witnessFilterText, value,
                x => FilterWitnesses(x));
        }
        #endregion

        #region IsObjectionsHidden
        private bool _isObjectionsHidden = true;
        public bool IsObjectionsHidden
        {
            get => _isObjectionsHidden;
            set => UpdateProperty(ref _isObjectionsHidden, value,
                v => _ = RefreshWitnessAsync());
        }

        #endregion

        #region IsSearchBusy
        private bool _isSearchBusy;
        public bool IsSearchBusy
        {
            get => _isSearchBusy;
            set => UpdateProperty(ref _isSearchBusy, value);
        }
        #endregion

        #region IsSearchInitializing
        private bool _isSearchInitializing;
        public bool IsSearchInitializing
        {
            get => _isSearchInitializing;
            set => UpdateProperty(ref _isSearchInitializing, value);
        }
        #endregion

        #region SelectedWitness
        private WitnessContent? _selectedWitness;
        public WitnessContent? SelectedWitness
        {
            get => _selectedWitness;
            set => UpdateProperty(ref _selectedWitness, value);
        }
        #endregion

        #region SearchResults
        private SearchResults? _searchResults = null;
        public SearchResults? SearchResults
        {
            get => _searchResults;
            set => UpdateProperty(ref _searchResults, value);
        }
        #endregion

        #region SelectedSearchMatch
        private SearchMatch? _selectedSearchMatch = null;
        public SearchMatch? SelectedSearchMatch
        {
            get => _selectedSearchMatch;
            set => UpdateProperty(ref _selectedSearchMatch, value);
        }
        #endregion

        #region WitnessContentSelectedAsyncCommand
        private Func<WitnessContent, Task>? _witnessContentSelectedAsyncCommand;
        public Func<WitnessContent, Task> WitnessContentSelectedAsyncCommand
        {
            get
            {
                return _witnessContentSelectedAsyncCommand ??= CreateEventCallbackAsyncCommand<WitnessContent>(item => WitnessContentSelectedAsync(item), "Unable to select witness item");
            }
        }
        #endregion

        #region HandleSearchKeypressAsyncCommand
        private Func<KeyboardEventArgs, Task>? _handleSearchKeypressAsyncCommand;
        public Func<KeyboardEventArgs, Task> HandleSearchKeypressAsyncCommand
        {
            get
            {
                return _handleSearchKeypressAsyncCommand ??= CreateEventCallbackAsyncCommand<KeyboardEventArgs>(args => HandleSearchKeypressAsync(args), "Unable to handle keypress");
            }
        }
        #endregion

        #region SearchAsyncCommand
        private Func<Task>? _searchAsyncCommand;
        public Func<Task> SearchAsyncCommand
        {
            get
            {
                return _searchAsyncCommand ??= CreateEventCallbackAsyncCommand(() => SearchAsync(), "Unable to search");
            }
        }
        #endregion

        #region SearchMatchSelectedAsyncCommand
        private Func<SearchMatch, Task>? _searchMatchSelectedAsyncCommand;
        public Func<SearchMatch, Task> SearchMatchSelectedAsyncCommand
        {
            get
            {
                return _searchMatchSelectedAsyncCommand ??= CreateEventCallbackAsyncCommand<SearchMatch>(item => SearchMatchSelectedAsync(item), "Unable to select search match");
            }
        }
        #endregion

        #region InitializePageFrameAsyncCommand
        private Func<Task>? _initializePageFrameAsyncCommand;
        public Func<Task> InitializePageFrameAsyncCommand
        {
            get
            {
                return _initializePageFrameAsyncCommand ??= CreateEventCallbackAsyncCommand(() => InitializePageFrameAsync(), "Unable to initialize page frame");
            }
        }
        #endregion

        #endregion

        #region Protected Methods

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("Initializing display page");
            await base.OnInitializedAsync();

            _browserInitialization.SetResult();
            WitnessSearch ??= new WitnessSearchService(_fileService!, () => RefreshAsync());
            Console.WriteLine("Display page initialization complete");

            await LoadWitnessesAsync();
            await LoadCurrentWitnessAsync("Overview", 0);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    Console.WriteLine("First render initialization");
                    await RefreshAsync();

                    Console.WriteLine("Initializing search");
                    await InitializeSearchAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to initialize: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine("First render initialization complete");
                }
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected string CleanMenuName(string name)
        {
            return HttpUtility.HtmlDecode(name);
        }

        protected WitnessContent GetWitnessByNumber(int number)
        {
            return AllWitnesses
                .First(x => x.WitnessNumber == number);
        }

        private async Task LoadWitnessesAsync()
        {
            if (AllWitnesses.Length > 0)
            {
                return;
            }

            var data = await _fileService!.LoadDataAsync("Witnesses\\witnesses.json");
            var json = Encoding.UTF8.GetString(data);

            var witnesses = json.DeserializeFromJson<WitnessContent[]>()!;
            AllWitnesses = witnesses;
            FilteredWitnesses = witnesses;
        }

        private async Task WitnessContentSelectedAsync(WitnessContent item)
        {
            await Task.Yield();
            Console.WriteLine($"Navigating to: {item.Name}");
            await LoadCurrentWitnessAsync(item.Name, item.WitnessNumber);
        }

        private Task HandleSearchKeypressAsync(KeyboardEventArgs args)
        {
            if (args.Code == "Enter" || args.Code == "Return" || args.Code == "NumpadEnter")
            {
                return SearchAsyncCommand!();
            }

            return Task.CompletedTask;
        }

        private async Task SearchAsync()
        {
            await Task.Yield();
            await SearchForTextAsync(SearchText ?? "");
        }

        private async Task SearchForTextAsync(string? text)
        {
            await Task.Yield();
            text = (text ?? "").Trim();

            if (text.StartsWith('"') && text.EndsWith('"'))
            {
                await FindExactMatchesAsync(text);
            }
            else
            {
                await FindPhraseMatchesAsync(text);
            }

            await RefreshAsync();
        }

        private async Task<bool> FindExactMatchesAsync(string search)
        {
            var matches = await ExecuteSearchAsync(() => WitnessSearch!.FindExactMatchesAsync(search));

            SearchResults = new SearchResults
            {
                AllMatches = matches,
                MatchMode = matches.Length > 0
                    ? SearchMatchMode.SearchMatches
                    : SearchMatchMode.NoMatches
            };

            Console.WriteLine($"Found {matches.Length} exact matches");
            await RefreshAsync();

            return matches.Length > 0;
        }

        private async Task<bool> FindPhraseMatchesAsync(string search)
        {
            var matches = await ExecuteSearchAsync(() => WitnessSearch!.FindPhraseMatchesAsync(search));

            SearchResults = new SearchResults
            {
                AllMatches = matches,
                MatchMode = matches.Length > 0
                    ? SearchMatchMode.SearchMatches
                    : SearchMatchMode.NoMatches
            };

            Console.WriteLine($"Found {matches.Length} exact matches");
            await RefreshAsync();
            return matches.Length > 0;
        }

        private async Task SearchMatchSelectedAsync(SearchMatch item)
        {
            await LoadCurrentWitnessAsync(item.Witness, item.WitnessNumber);
            await Task.Yield();

            await HighlightXPathLocationsAsync(new[] { item.XPath }, true);
            SelectedSearchMatch = item;
        }

        private void FilterWitnesses(string filter)
        {
            var filteredWitnesses = AllWitnesses
                .Where(x => string.IsNullOrWhiteSpace(filter) || $"{x.WitnessNumber} - {x.Name}".Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            FilteredWitnesses = filteredWitnesses;
        }

        private async Task InitializeSearchAsync()
        {
            IsSearchInitializing = true;
            await RefreshAsync();
            await Task.Delay(15);

            await WitnessSearch!.InitializeAsync();

            IsSearchInitializing = false;
            await RefreshAsync();
            await Task.Delay(15);
        }

        private async Task<SearchMatch[]> ExecuteSearchAsync(Func<Task<SearchMatch[]>> action)
        {
            IsSearchBusy = true;
            await RefreshAsync();
            await Task.Delay(15);

            var matches = await action();

            IsSearchBusy = false;
            await RefreshAsync();
            await Task.Delay(15);

            return matches;
        }

        private async Task InitializePageFrameAsync()
        {
            Console.WriteLine("Initializing page frame");

            try
            {
                await _jsRuntime!.InvokeAsync<object>("initializePageFrame", IsObjectionsHidden)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to initialize page frame: {ex.Message}");
            }
            finally
            {
                _browserInitialization.TrySetResult();
                Console.WriteLine("Page frame initialization complete");
            }
        }

        private async Task HighlightXPathLocationsAsync(string[] xpath, bool isHighlight)
        {
            var json = xpath.SerializeToJson();

            for (var x = 0; x < 50; x++)
            {
                try
                {
                    var result = await _jsRuntime
                        !.InvokeAsync<bool>("highlightSearchResults", json, isHighlight)
                        .ConfigureAwait(false);

                    if (result)
                    {
                        return;
                    }
                }
                catch
                {
                    // Nothing
                }

                await Task.Delay(100);
            }
        }

        private async Task LoadCurrentWitnessAsync(string name, int number)
        {
            // Check if witness already loaded
            if (SelectedWitness != null && SelectedWitness.WitnessNumber == number)
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Loading witness: {name}");
            _browserInitialization = new TaskCompletionSource();

            SelectedWitness = AllWitnesses
                .First(x => x.WitnessNumber == number);

            await Task.Yield();
            await RefreshAsync();

            // Wait for the initialization to complete
            await Task.WhenAny(_browserInitialization.Task, Task.Delay(5000));
            Console.WriteLine($"Witness loaded: {timer.ElapsedMilliseconds} ms");
        }

        private async Task RefreshWitnessAsync()
        {
            var currentWitness = SelectedWitness;
            SelectedWitness = null;

            await Task.Yield();
            await RefreshAsync();

            SelectedWitness = currentWitness;

            await Task.Yield();
            await RefreshAsync();
        }

        #endregion
    }
}
