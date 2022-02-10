using System;
using System.Numerics;
using ImGuiNET;
using Wordsmith.Helpers;

namespace Wordsmith.Gui
{
    public class ThesaurusUI : WordsmithWindow
    {
        protected string _search = "";
        protected string _query = "";
        protected bool _searchFailed = false;
        protected int _searchMinLength = 3;

        protected SearchHelper SearchHelper;

        public ThesaurusUI() : base($"{Wordsmith.AppName}##thesaurus")
        {
            IsOpen = true;
            SearchHelper = new SearchHelper();
            _search = "";

            WordsmithUI.WindowSystem.AddWindow(this);
            SizeConstraints = new WindowSizeConstraints()
            {
                MinimumSize = new(375, 330),
                MaximumSize = new(float.MaxValue, float.MaxValue)
            };

            Flags |= ImGuiWindowFlags.NoScrollbar;
            Flags |= ImGuiWindowFlags.NoScrollWithMouse;
            Flags |= ImGuiWindowFlags.MenuBar;
        }
        public override void Draw()
        {
            if (!IsOpen) return;

            DrawMenu();
            DrawWordSearch();
        }

        protected void DrawMenu()
        {
            try
            {
                // If we fail to create a menu bar, back out
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu($"Character##{Wordsmith.AppName}CharacterMenu"))
                    {
                        if(ImGui.MenuItem($"New##{Wordsmith.AppName}NewCharacterMenuItem"))

                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem($"Settings##{Wordsmith.AppName}SettingsMenuItem"))
                    {
                        //Plugin.PluginUi.SettingsUI.IsOpen = !Plugin.PluginUi.SettingsUI.IsOpen;
                        WordsmithUI.ShowSettings();
                    }

                    // Close the menu bar.
                    ImGui.EndMenuBar();
                }
            }
            catch(Exception ex)
            {
                Dalamud.Logging.PluginLog.Log(ex.Message);
            }
        }


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
                        DrawLastSearch();

                        foreach (Data.WordSearchResult result in SearchHelper.History)
                            DrawSearchResult(result);

                        // End the child element.
                        ImGui.EndChild();
                    }
                    ImGui.EndChild();
                }
            }
            catch (Exception e)
            {
                //Plugin.PluginUi.RaiseAlert(e.Message);
            }
        }

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
        protected void DrawSearchBar()
        {
            if (ImGui.BeginTable("SearchZoneTable", 2))
            {
                ImGui.TableSetupColumn("SearchTextBarColumn");
                ImGui.TableSetupColumn("SearchTextButtonColumn", ImGuiTableColumnFlags.WidthFixed, 50);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);

                if (ImGui.InputTextWithHint("###ThesaurusSearchBar", "Enter a word and hit enter to search...", ref _search, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                    _searchFailed = !ScheduleSearch();

                ImGui.TableNextColumn();
                if (ImGui.Button("Search", new Vector2(50, 20)))
                    _searchFailed = !ScheduleSearch();

                ImGui.EndTable();

                if (_searchFailed)
                    ImGui.TextColored(new(255, 0, 0, 255), $"Minimum of {_searchMinLength} letters required.");

                ImGui.Separator();
            }            
        }

        protected void DrawLastSearch()
        {
            if (SearchHelper.Result?.SearchError ?? false)
            {
                ImGui.TextColored(new Vector4(255, 0, 0, 255), $"Failed to acquire results for {SearchHelper.Result.Query}\n{SearchHelper.Result.Exception?.Message ?? ""}");
                ImGui.Separator();
                ImGui.Spacing();
            }
            else if (SearchHelper.Result != null)
                DrawSearchResult(SearchHelper.Result);
        }

        protected void DrawSearchResult(Data.WordSearchResult result)
        {
            if (result != null)
            {
                if (ImGui.CollapsingHeader($"{result.Query.Trim().CaplitalizeFirst()}##{result.ID}", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Indent();
                    foreach (Data.ThesaurusEntry entry in result?.Entries ?? Array.Empty<Data.ThesaurusEntry>())
                        DrawEntry(entry);

                    if (ImGui.Button(" Delete Result ", new Vector2(-1, 20)))
                        SearchHelper.DeleteResult(result);
                    
                    ImGui.Unindent();
                }
            }
        }

        protected void DrawEntry(Data.ThesaurusEntry entry)
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
        public override void Update()
        {
            base.Update();
            if (!IsOpen)
                Dispose();

            // If not querying return.
            if (_query == "" || _query == "##done##") return;
            SearchHelper.SearchThesaurus(_query.Trim());
            _query = "##done##";
        }

        public override void Dispose()
        {
            base.Dispose();
            SearchHelper.Dispose();

            // Remove the window from the window system.
            WordsmithUI.WindowSystem.RemoveWindow(this);
        }
    }
}
