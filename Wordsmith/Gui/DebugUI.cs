
#if DEBUG
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
namespace Wordsmith.Gui;
internal sealed class DebugUI : Window
{
    internal static bool bOverrideSpellcheckLimited = false;
    public DebugUI() : base( $"{Wordsmith.APPNAME} - Debug" )
    {
        this.SizeConstraints = new()
        {
            MinimumSize = new(200, 200),
            MaximumSize = new(9999, 9999)
        };

        this.Flags |= ImGuiWindowFlags.HorizontalScrollbar;

        Wordsmith.PluginLog.Debug( $"DebugUI created." );
    }
    private static string _consoleInput = "";
    private static int _consolePadNumber = 0;

    public override void Draw()
    {
        ImGui.Text( $"Delta: {WordsmithUI.Clock.Delta}" );
        ImGui.Text( $"Longest Delta: {WordsmithUI.Clock.LongestFrame}" );
        if ( ImGui.Button( $"Reset Delta##DebugButton", new( -1, Wordsmith.BUTTON_Y.Scale() ) ) )
            WordsmithUI.Clock.ResetLongest();

        if ( ImGui.CollapsingHeader( $"Wordsmith Configuration##DebugUIHeDebugUICollapsingHeaderader" ) )
            DrawClassData( Wordsmith.Configuration, $"Configuration" );

        if ( ImGui.CollapsingHeader( $"Web manifest##DebugUIHeDebugUICollapsingHeaderader" ) )
            DrawClassData( Wordsmith.WebManifest, $"Web Manifest" );

        // If there is a setting, draw the settings section.
        if ( ImGui.CollapsingHeader( "Settings Data##DebugUIHeDebugUICollapsingHeaderader" ) )
            DrawClassData( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( SettingsUI ) ), SettingsUI.GetWindowName() );        

        // If there is a thesaurus, draw the thesaurus section.
        if ( ImGui.CollapsingHeader( "Thesaurus Data##DebugUICollapsingHeader" ) )
            DrawClassData( WordsmithUI.Windows.FirstOrDefault( x => x.GetType() == typeof( ThesaurusUI ) ), ThesaurusUI.GetWindowName() );

        // If there is a MessageBox, draw the MessageBox section.
        if ( ImGui.CollapsingHeader( $"Message Boxes##DebugUICollapsingHeader" ) )
        {
            ImGui.Indent();
            foreach ( MessageBox mb in WordsmithUI.Windows.Where(w => w.GetType() == typeof(MessageBox)) )
                if ( ImGui.CollapsingHeader($"{mb.WindowName}") )
                    DrawClassData( mb, $"MessageBoxUI" );

            foreach ( ErrorWindow ew in WordsmithUI.Windows.Where( w => w.GetType() == typeof( ErrorWindow ) ) )
                if ( ImGui.CollapsingHeader( $"{ew.WindowName}" ) )
                    DrawClassData( ew, $"ErrorWindowUI" );
            ImGui.Unindent();
        }

        // If there is a scratch pad, draw the scratch pad section.
        if ( ImGui.CollapsingHeader( "Scratch Pad Data##DebugUICollapsingHeader" ) )
        {
            ImGui.Indent();
            foreach ( ScratchPadUI pad in WordsmithUI.Windows.Where( w => w.GetType() == typeof( ScratchPadUI ) ) )
                if ( ImGui.CollapsingHeader($"Scratch Pad {pad.ID}##DebugUICollapsingHeader") )
                    DrawClassData( pad, $"ScratchPad{pad.ID}", "NextID" );
            ImGui.Unindent();
        }
        
        if ( ImGui.CollapsingHeader( "Console##ConsoleCollapsingHeader" ) )
        {
            ImGui.Indent();
            string[] options = WordsmithUI.Windows.Where(x => x is ScratchPadUI).Select(x => x.WindowName).ToArray();
            if ( _consolePadNumber >= options.Length )
                _consolePadNumber = 0;

            Window? w = null;
            if ( options.Length == 0 )
            {
                ImGui.BeginDisabled();
                ImGui.Combo( "##ScratchPadConsoleSelectionCombo", ref _consolePadNumber, new string[] { "None" }, 1 );
            }
            else
            {
                ImGui.Combo( "##ScratchPadConsoleSelectionCombo", ref _consolePadNumber, options, options.Length );
                w = WordsmithUI.GetWindow( options[_consolePadNumber] );
            }

            if ( ImGui.BeginChildFrame(99, new (ImGui.GetContentRegionAvail().X, WordsmithUI.LineHeight * 15 - Wordsmith.BUTTON_Y.Scale()*2 ) ) )
            {
                if ( w is ScratchPadUI pad && Helpers.Console.Log.Keys.Contains(pad))
                    ImGui.TextWrapped( string.Join("\n", Helpers.Console.Log[pad] ) );
            }
            ImGui.EndChildFrame();

            ImGui.SetNextItemWidth( -1 );
            if ( ImGui.InputText( $"##ConsoleInputLine", ref _consoleInput, 1024, ImGuiInputTextFlags.EnterReturnsTrue ) )
            {
                if ( w is ScratchPadUI pad )
                    Helpers.Console.ProcessCommand( pad, $"devx {_consoleInput}" );
                _consoleInput = "";
            }
            if ( options.Length == 0 )
                ImGui.EndDisabled();            
        }
    }
    private static void DrawClassData( object? obj, object id, params string[]? excludes )
    {
        if ( obj == null )
            return;

        ImGui.Indent();
        // Get the list of results
        IReadOnlyList<(int Type, string Name, string Value) > data = obj.GetProperties(excludes);

        // Draw Properties
        if ( ImGui.CollapsingHeader( $"Properties##{id}DebugUIHeader" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 0 ) )
                ImGui.TextWrapped( $"{Name}\t: {Value.Replace("\r", "\\r").Replace("\n", "\\n")}" );
            ImGui.Unindent();
        }

        // Draw Fields
        if ( ImGui.CollapsingHeader( $"Fields##{id}DebugUIHeader" ) )
        {
            ImGui.Indent();
            foreach ( (int Type, string Name, string Value) in data.Where( d => d.Type == 1 ) )
                ImGui.TextWrapped( $"{Name}\t: {Value.Replace( "\r", "\\r" ).Replace( "\n", "\\n" )}" );
            ImGui.Unindent();
        }
        ImGui.Unindent();
    }
}
#endif
