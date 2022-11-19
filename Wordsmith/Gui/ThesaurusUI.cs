using ImGuiNET;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Wordsmith.Helpers;
using Wordsmith;

namespace Wordsmith.Gui;

public class ThesaurusUI : Window, IReflected
{
    protected string _search = "";
    protected string _query = "";
    protected bool _searchFailed = false;
    protected int _searchMinLength = 3;

    protected SearchHelper SearchHelper;

    /// <summary>
    /// Instantiates a new ThesaurusUI object.
    /// </summary>
    public ThesaurusUI() : base($"{Wordsmith.AppName} - Thesaurus")
    {
        SearchHelper = new SearchHelper();
        _search = "";

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(375, 330),
            MaximumSize = ImGuiHelpers.ScaledVector2(9999, 9999)
        };

        Flags |= ImGuiWindowFlags.NoScrollbar;
        Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        //Flags |= ImGuiWindowFlags.MenuBar;
    }

    #region Drawing
    /// <summary>
    /// The Draw entry point for Dalamud.Interface.Windowing
    /// </summary>
    public override void Draw()
    {
        // Create a child element for the word search.
        if ( ImGui.BeginChild( "Word Search Window" ) )
        {
            DrawSearchBar();
            if ( ImGui.BeginChild( "SearchResultWindow" ) )
            {
                DrawSearchErrors();
                foreach ( WordSearchResult result in SearchHelper.History )
                    DrawSearchResult( result );

                // End the child element.
                ImGui.EndChild();
            }
            ImGui.EndChild();
        }
    }

    /// <summary>
    /// Draws the search bar on the UI.
    /// </summary>
    protected void DrawSearchBar()
    {
        float btnWidth = 100*ImGuiHelpers.GlobalScale;
        string searchQuery = "";
        ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionMax().X - btnWidth - ImGui.GetStyle().FramePadding.X*2);
        if ( ImGui.InputTextWithHint( "###ThesaurusSearchBar", "Search...", ref searchQuery, 128, ImGuiInputTextFlags.EnterReturnsTrue ) )
            _searchFailed = !ScheduleSearch( searchQuery );

        ImGui.SameLine();
        if ( ImGui.Button("Search##ThesaurusSearchButton", new(btnWidth, 0)))

        if (_searchFailed)
            ImGui.TextColored(new(255, 0, 0, 255), $"Minimum of {_searchMinLength} letters required.");

        ImGui.Separator();
    }

    /// <summary>
    /// Draws the last search's data to the UI.
    /// </summary>
    protected void DrawSearchErrors()
    {
        //if (SearchHelper.Error != null)
        //{
        //    ImGui.TextColored(new Vector4(255, 0, 0, 255), $"Search Error:\n{SearchHelper.Error.Message}");
        //    ImGui.Separator();
        //    ImGui.Spacing();
        //}
    }

    /// <summary>
    /// Draws one search result item to the UI.
    /// </summary>
    /// <param name="result">The search result to be drawn</param>
    protected void DrawSearchResult(WordSearchResult result)
    {
        if (result != null)
        {
            bool vis = true;
            if (ImGui.CollapsingHeader($"{result.Query.Trim().CaplitalizeFirst()}##{result.ID}", ref vis, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                foreach (ThesaurusEntry entry in result?.Entries ?? Array.Empty<ThesaurusEntry>())
                    DrawEntry(entry);

                if(!vis)
                    SearchHelper.DeleteResult(result);
                
                ImGui.Unindent();
            }
        }
    }

    /// <summary>
    /// Draws a search result's entry data. One search result can have multiple data entries.
    /// </summary>
    /// <param name="entry">The data to draw.</param>
    protected void DrawEntry(ThesaurusEntry entry)
    {
        if (ImGui.CollapsingHeader(
                    char.ToUpper((entry.Type.Trim())[0]).ToString() + entry.Type.Substring(1) + $"##{entry.ID}"))
        {
            ImGui.Indent();
            if (entry.Definition.Length > 0)
                ImGui.TextWrapped("Definition: " + entry.Definition);                

            if (entry.Synonyms.Length > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();                    
                ImGui.TextWrapped("Synonyms");
                ImGui.TextWrapped(entry.SynonymString);
            }

            if (entry.Related.Length > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.TextWrapped("Related Words");
                ImGui.TextWrapped(entry.RelatedString);
            }

            if (entry.NearAntonyms.Length > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.TextWrapped("Near Antonyms");
                ImGui.TextWrapped(entry.NearAntonymString);
            }

            if (entry.Antonyms.Length > 0)
            {
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.TextWrapped("Antonyms");
                ImGui.TextWrapped(entry.AntonymString);
            }
            ImGui.Unindent();
        }
    }
    #endregion

    /// <summary>
    /// Moves the search string into the query to search for it.
    /// </summary>
    /// <returns>Returns true if the search string passes the minimum length.</returns>
    protected bool ScheduleSearch(string query)
    {
        if ( query.Length >= _searchMinLength )
        { 
            SearchHelper.SearchThesaurus( query );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Framework update.
    /// </summary>
    public override void Update()
    {
        base.Update();

        // If not querying return.
        if (_query == "" || _query == "##done##") return;
        SearchHelper.SearchThesaurus(_query.Trim());
        _query = "##done##";
    }

    /// <summary>
    ///  Disposes of the SearchHelper child.
    /// </summary>
    public void DisposeChild() => this.SearchHelper.Dispose();
}
