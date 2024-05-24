using System.Collections.Generic;
using BuurtApplicatie.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuurtApplicatie.Models.PostOverviewViewModels
{
    public class PostOverviewViewModel
    {
        public PaginatedList<PostOverviewPostViewModel> Posts { get; set; }
        
        public IEnumerable<CategoryViewModel> Categories { get; set; }
        
        public List<StatusCheckBox> StatusCheckBoxes { get; } = new List<StatusCheckBox>
        {
            new StatusCheckBox { Value = StatusCheckBox.Open, Label = "Open" },
            new StatusCheckBox { Value = StatusCheckBox.Closed, Label = "Gesloten"  }
        };
        
        public string OrderBy { get; set; }

        public List<SelectListItem> OrderBySelectListItems { get; } = new List<SelectListItem>
        {
            new SelectListItem {Text = "Datum (aflopend)", Value = "date_desc"},
            new SelectListItem {Text = "Datum (oplopend)", Value = "date_asc"},
            new SelectListItem {Text = "Likes (oplopend)", Value = "likes_asc"},
            new SelectListItem {Text = "Likes (aflopend)", Value = "likes_desc"},
            new SelectListItem {Text = "Views (oplopend)", Value = "views_asc"},
            new SelectListItem {Text = "Views (aflopend)", Value = "views_desc"}
        };
        
        public FavoritesCheckBox InMyFavorites { get; } = new FavoritesCheckBox { Label = "In mijn favorieten"};
    }

    public class FavoritesCheckBox
    {
        public string Label { get; set; }
        
        public bool IsChecked { get; set; }
    }

    public class StatusCheckBox
    {
        public const string Open = "Open";
        public const string Closed = "Closed";
        public string Label { get; set; }
        
        public string Value { get; set; }
        
        public bool IsChecked { get; set; }
    }
}