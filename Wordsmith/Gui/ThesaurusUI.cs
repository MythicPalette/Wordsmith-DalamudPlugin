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

    /// <summary>
    /// The Draw entry point for Dalamud.Interface.Windowing
    /// </summary>
    public override void Draw()
    {
        DrawWordSearch();
    }

    /// <summary>
    /// Draws main UI.
    /// </summary>
    protected void DrawWordSearch()
    {
        try
        {
            // Create a child element for the word search.
            if (ImGui.BeginChild("Word Search Window"))
            {
                DrawSearchBar();
                if (ImGui.BeginChild("SearchResultWindow"))
                {
                    DrawSearchErrors();
                    foreach (Wordsmith.WordSearchResult result in SearchHelper.History)
                        DrawSearchResult(result);

                    // End the child element.
                    ImGui.EndChild();
                }
                ImGui.EndChild();
            }
        }
        catch (Exception)
        {
            //Plugin.PluginUi.RaiseAlert(e.Message);
        }
    }

    /// <summary>
    /// Moves the search string into the query to search for it.
    /// </summary>
    /// <returns>Returns true if the search string passes the minimum length.</returns>
    protected bool ScheduleSearch()
    {
        if (_search.Length >= _searchMinLength)
        {
            //SearchHelper.SearchThesaurus(_search.Trim());
            _query = _search;
            _search = "";
            return true;
        }
        return false;
    }

    /// <summary>
    /// Draws the search bar on the UI.
    /// </summary>
    protected void DrawSearchBar()
    {
        if (ImGui.BeginTable("SearchZoneTable", 2))
        {
            ImGui.TableSetupColumn("SearchTextBarColumn");
            ImGui.TableSetupColumn("SearchTextButtonColumn", ImGuiTableColumnFlags.WidthFixed, 50 * ImGuiHelpers.GlobalScale);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);

            if (ImGui.InputTextWithHint("###ThesaurusSearchBar", "Enter a word and hit enter to search...", ref _search, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                _searchFailed = !ScheduleSearch();

            ImGui.TableNextColumn();
            if (ImGui.Button("Search", ImGuiHelpers.ScaledVector2(50, 20)))
                _searchFailed = !ScheduleSearch();

            ImGui.EndTable();

            if (_searchFailed)
                ImGui.TextColored(new(255, 0, 0, 255), $"Minimum of {_searchMinLength} letters required.");

            ImGui.Separator();
        }            
    }

    /// <summary>
    /// Draws the last search's data to the UI.
    /// </summary>
    protected void DrawSearchErrors()
    {
        if (SearchHelper.Error != null)
        {
            ImGui.TextColored(new Vector4(255, 0, 0, 255), $"Search Error:\n{SearchHelper.Error.Message}");
            ImGui.Separator();
            ImGui.Spacing();
        }
    }

    /// <summary>
    /// Draws one search result item to the UI.
    /// </summary>
    /// <param name="result">The search result to be drawn</param>
    protected void DrawSearchResult(Wordsmith.WordSearchResult result)
    {
        if (result != null)
        {
            bool vis = true;
            if (ImGui.CollapsingHeader($"{result.Query.Trim().CaplitalizeFirst()}##{result.ID}", ref vis, ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                foreach (Wordsmith.ThesaurusEntry entry in result?.Entries ?? Array.Empty<Wordsmith.ThesaurusEntry>())
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
    protected void DrawEntry(Wordsmith.ThesaurusEntry entry)
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
