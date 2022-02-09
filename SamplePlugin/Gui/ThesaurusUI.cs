using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ImGuiNET;
using Wordsmith.Helpers;

namespace Wordsmith.Gui
{
    public class ThesaurusUI : Window
    {
        protected string _search = "";

        protected SearchHelper SearchHelper;

        public ThesaurusUI(Plugin plugin) : base(plugin)
        {
            SearchHelper = new SearchHelper(plugin);
            _search = "";
        }
        protected override void DrawUI()
        {
            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(9999, 9999));
            if (ImGui.Begin($"{Plugin.Name} - Thesaurus", ref this._visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                //DrawWordSearch();
            }
            ImGui.End();
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
                Plugin.PluginUi.RaiseAlert(e.Message);
            }
        }

        protected bool DoSearch()
        {
            if (_search.ToLower().Trim() == "##debug##")
            {
                Plugin.Debug = !Plugin.Debug;
                _search = "";
                return true;
            }
            else if (_search.Length > 2)
            {
                SearchHelper.SearchThesaurus(_search.Trim());
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

                bool SearchFail = false;
                if (ImGui.InputTextWithHint("###ThesaurusSearchBar", (Plugin.Debug ? "Debugging..." : "Enter a word and hit enter to search..."), ref _search, 128, ImGuiInputTextFlags.EnterReturnsTrue))
                    SearchFail = DoSearch();

                ImGui.TableNextColumn();
                if (ImGui.Button("Search", new Vector2(50, 20)))
                    SearchFail = DoSearch();

                if(SearchFail)
                    ImGui.Text("Minimum of 2 letters required.");

                ImGui.EndTable();
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
    }
}
