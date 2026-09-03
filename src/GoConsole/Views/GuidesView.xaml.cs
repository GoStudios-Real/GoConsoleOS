using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class GuidesView : UserControl
{
    private readonly LibraryData? _library;
    private string _currentFilter = "all";
    private List<GuideItem> _allGuides = new();

    public GuidesView()
    {
        InitializeComponent();
        _library = null;
        LoadGuides();
    }

    public GuidesView(LibraryData? library)
    {
        InitializeComponent();
        _library = library;
        LoadGuides();
    }

    private void LoadGuides()
    {
        _allGuides = new List<GuideItem>();

        if (_library != null)
        {
            foreach (var game in _library.Games.Take(20))
            {
                _allGuides.AddRange(GenerateGuidesForGame(game));
            }
        }

        _allGuides.AddRange(GetFeaturedGuides());

        if (_allGuides.Count == 0)
        {
            _allGuides.Add(new GuideItem
            {
                GameTitle = "GoConsoleOS",
                Title = "Getting Started with GoConsoleOS",
                Category = "Getting Started",
                Description = "Learn how to navigate the console, configure performance modes, and launch your favorite games.",
                CategoryBrush = CategoryBrushes["beginner"]
            });
        }

        ApplyFilter("all");
    }

    private IEnumerable<GuideItem> GenerateGuidesForGame(GameInfo game)
    {
        var name = game.Title;
        var platform = game.Platform;

        yield return new GuideItem
        {
            GameTitle = name,
            Title = $"Essential Tips for {name}",
            Category = "Tips & Tricks",
            Description = $"Master {name} with these pro tips covering combat, exploration, and resource management on {platform}.",
            CategoryBrush = CategoryBrushes["tips"]
        };

        yield return new GuideItem
        {
            GameTitle = name,
            Title = $"{name} Beginner's Guide",
            Category = "Getting Started",
            Description = $"New to {name}? Start here with the basics: controls, UI overview, and first steps to get you going.",
            CategoryBrush = CategoryBrushes["beginner"]
        };

        yield return new GuideItem
        {
            GameTitle = name,
            Title = $"Hidden Secrets in {name}",
            Category = "Hidden Secrets",
            Description = $"Discover easter eggs, secret areas, and unlockable content hidden throughout {name}.",
            CategoryBrush = CategoryBrushes["secrets"]
        };

        if (new Random(name.GetHashCode()).Next(0, 2) == 0)
        {
            yield return new GuideItem
            {
                GameTitle = name,
                Title = $"Complete {name} Walkthrough",
                Category = "Walkthrough",
                Description = $"A step-by-step walkthrough covering every level, boss fight, and collectible in {name}.",
                CategoryBrush = CategoryBrushes["walkthrough"]
            };
        }
    }

    private List<GuideItem> GetFeaturedGuides()
    {
        return new List<GuideItem>
        {
            new()
            {
                GameTitle = "GoConsoleOS",
                Title = "Optimizing Performance Settings",
                Category = "Tips & Tricks",
                Description = "Get the best frame rates and visual quality by tuning your performance mode for each game.",
                CategoryBrush = CategoryBrushes["tips"]
            },
            new()
            {
                GameTitle = "GoConsoleOS",
                Title = "Controller Setup Guide",
                Category = "Getting Started",
                Description = "Pair and configure your Xbox, PlayStation, or third-party controller for GoConsoleOS.",
                CategoryBrush = CategoryBrushes["beginner"]
            },
            new()
            {
                GameTitle = "GoConsoleOS",
                Title = "Cloud Saves & Multi-Machine Sync",
                Category = "Walkthrough",
                Description = "Set up cloud save sync across multiple GoConsoleOS installations on your home network.",
                CategoryBrush = CategoryBrushes["walkthrough"]
            }
        };
    }

    private void ApplyFilter(string filter)
    {
        _currentFilter = filter.ToLowerInvariant();

        var label = filter.ToUpper();
        GuidesTitle.Text = label == "ALL" ? "ALL GUIDES" : label + " GUIDES";

        var filtered = _currentFilter switch
        {
            "all" => _allGuides,
            _ => _allGuides.Where(g =>
                g.Category.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase)).ToList()
        };

        GuidesGrid.ItemsSource = filtered;
        GuideCountDetail.Text = $"{filtered.Count} guide{(filtered.Count == 1 ? "" : "s")} found";
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            ApplyFilter(tag);
    }

    private static readonly Dictionary<string, Brush> CategoryBrushes = new()
    {
        ["beginner"] = new SolidColorBrush(Color.FromRgb(0, 102, 255)),
        ["tips"] = new SolidColorBrush(Color.FromRgb(255, 185, 64)),
        ["walkthrough"] = new SolidColorBrush(Color.FromRgb(136, 192, 255)),
        ["secrets"] = new SolidColorBrush(Color.FromRgb(255, 107, 141)),
        ["all"] = new SolidColorBrush(Color.FromRgb(200, 200, 255))
    };

    private class GuideItem
    {
        public string GameTitle { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public Brush? CategoryBrush { get; set; }
    }
}
