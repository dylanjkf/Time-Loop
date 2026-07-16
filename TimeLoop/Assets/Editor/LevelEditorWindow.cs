using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TimeLoop.EditorTools
{
    /// <summary>
    /// Visual authoring tool for the shared .level text format consumed by
    /// TimeLoop.Levels.LevelParser (see docs/TECHNICAL_ARCHITECTURE.md section 6). Lets a
    /// designer paint a grid of legend characters and fill in the header fields, then writes a
    /// .level file matching LevelParser's exact expectations.
    ///
    /// Row convention: LevelParser.BuildWorld reads the GRID: block top-to-bottom with the first
    /// row in the file mapped to the highest Y ("y = gridRows.Count - 1 - rowIndex"), so this
    /// window paints and saves rows from y = height - 1 down to y = 0 to match.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        private static readonly char[] Palette = { '.', '#', 'S', 'G', '1', '2', '3', 'X', 'B' };

        private const string DefaultSaveDirectorySuffix = "Resources/Levels/";

        private int _width = 10;
        private int _height = 8;
        private char[,] _grid;

        private string _levelName = "New Level";
        private int _world = 1;
        private float _loopSeconds = 20f;
        private int _maxTimelines = 1;
        private int _parLoops = 2;
        private int _parTicks = 100;
        private string _legendLine = "S=spawn G=goal 1=plate:p1";

        private Vector2 _scrollPosition;

        [MenuItem("Time Loop/Level Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(440f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_grid == null)
            {
                AllocateGrid();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _width = Mathf.Max(1, EditorGUILayout.IntField("Width", _width));
            _height = Mathf.Max(1, EditorGUILayout.IntField("Height", _height));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("New Grid"))
            {
                AllocateGrid();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Click a cell to cycle: . # S G 1 2 3 X(hazard) B(box)", EditorStyles.miniLabel);

            DrawGrid();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Header", EditorStyles.boldLabel);

            _levelName = EditorGUILayout.TextField("Name", _levelName);
            _world = EditorGUILayout.IntField("World", _world);
            _loopSeconds = EditorGUILayout.FloatField("Loop Seconds", _loopSeconds);
            _maxTimelines = EditorGUILayout.IntField("Max Timelines", _maxTimelines);
            _parLoops = EditorGUILayout.IntField("Par Loops", _parLoops);
            _parTicks = EditorGUILayout.IntField("Par Ticks", _parTicks);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Type the LEGEND: line yourself, matching the palette characters painted above " +
                "(e.g. \"S=spawn G=goal 1=plate:p1 D=door:and:p1\"). Tokens are space-separated " +
                "char=token pairs; see LevelParser's token vocabulary for the full list.",
                MessageType.None);
            _legendLine = EditorGUILayout.TextField("Legend Line", _legendLine);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_grid == null))
            {
                if (GUILayout.Button("Save As .level", GUILayout.Height(28f)))
                {
                    SaveLevel();
                }
            }
        }

        private void AllocateGrid()
        {
            _width = Mathf.Max(1, _width);
            _height = Mathf.Max(1, _height);

            _grid = new char[_width, _height];

            for (var x = 0; x < _width; x++)
            {
                for (var y = 0; y < _height; y++)
                {
                    _grid[x, y] = (x == 0 || y == 0 || x == _width - 1 || y == _height - 1) ? '#' : '.';
                }
            }
        }

        private void DrawGrid()
        {
            if (_grid == null) return;

            var gridWidth = _grid.GetLength(0);
            var gridHeight = _grid.GetLength(1);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(320f));

            // Paint from the top row (highest Y) down to y = 0, matching the saved file layout.
            for (var y = gridHeight - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();

                for (var x = 0; x < gridWidth; x++)
                {
                    var cellLabel = _grid[x, y].ToString();
                    if (GUILayout.Button(cellLabel, GUILayout.Width(20f), GUILayout.Height(20f)))
                    {
                        _grid[x, y] = CyclePalette(_grid[x, y]);
                    }
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static char CyclePalette(char current)
        {
            var index = Array.IndexOf(Palette, current);
            var next = (index + 1) % Palette.Length;
            return Palette[next];
        }

        private void SaveLevel()
        {
            if (_grid == null)
            {
                Debug.LogWarning("LevelEditorWindow: no grid allocated yet. Click 'New Grid' first.");
                return;
            }

            var defaultDirectory = Path.Combine(Application.dataPath, DefaultSaveDirectorySuffix);
            if (!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }

            var path = EditorUtility.SaveFilePanel("Save Level", defaultDirectory, "NewLevel", "level");
            if (string.IsNullOrEmpty(path)) return;

            var contents = BuildLevelText();

            try
            {
                File.WriteAllText(path, contents);
            }
            catch (Exception exception)
            {
                Debug.LogError($"LevelEditorWindow: failed to write level file to '{path}': {exception}");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"LevelEditorWindow: saved level '{_levelName}' to {path}.");
        }

        private string BuildLevelText()
        {
            var invariant = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.Append("NAME: ").Append(_levelName).Append('\n');
            sb.Append("WORLD: ").Append(_world.ToString(invariant)).Append('\n');
            sb.Append("LOOP_SECONDS: ").Append(_loopSeconds.ToString(invariant)).Append('\n');
            sb.Append("MAX_TIMELINES: ").Append(_maxTimelines.ToString(invariant)).Append('\n');
            sb.Append("PAR_LOOPS: ").Append(_parLoops.ToString(invariant)).Append('\n');
            sb.Append("PAR_TICKS: ").Append(_parTicks.ToString(invariant)).Append('\n');
            sb.Append("GRID:").Append('\n');

            var gridWidth = _grid.GetLength(0);
            var gridHeight = _grid.GetLength(1);

            // LevelParser.BuildWorld assigns the first GRID row in the file to
            // y = gridRows.Count - 1 (rowIndex 0 -> highest Y), so rows must be written from
            // y = height - 1 down to y = 0 for the saved grid to match what was painted above.
            for (var y = gridHeight - 1; y >= 0; y--)
            {
                for (var x = 0; x < gridWidth; x++)
                {
                    sb.Append(_grid[x, y]);
                }

                sb.Append('\n');
            }

            sb.Append("LEGEND:").Append('\n');
            sb.Append(_legendLine).Append('\n');

            return sb.ToString();
        }
    }
}
