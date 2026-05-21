using Godot;

namespace MineSweeper;

public partial class Cell : Control
{
    [Signal]
    public delegate void LeftClickedEventHandler();

    [Signal]
    public delegate void RightClickedEventHandler();

    [Signal]
    public delegate void ChordClickedEventHandler();

    public int GridX { get; set; }
    public int GridY { get; set; }

    private TextureRect _textureRect;

    private static AtlasTexture?[]? _numberTextures;
    private static AtlasTexture? _hideTexture;
    private static AtlasTexture? _flagTexture;
    private static AtlasTexture? _questionTexture;
    private static AtlasTexture? _mineTexture;
    private static AtlasTexture? _explodeTexture;
    private static AtlasTexture? _errorTexture;
    private static bool _texturesLoaded;

    public Cell() {
        CustomMinimumSize = new Vector2(25, 25);
        MouseFilter = MouseFilterEnum.Stop;

        LoadTexturesOnce();

        _textureRect = new TextureRect();
        _textureRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        AddChild(_textureRect);

        SetDisplay(CellState.Hidden, -1);
    }

    private static void LoadTexturesOnce() {
        if (_texturesLoaded) {
            return;
        }
        _texturesLoaded = true;

        _numberTextures = new AtlasTexture?[9];
        for (int i = 0; i <= 8; i++) {
            _numberTextures[i] =
                GD.Load<AtlasTexture>($"res://resources/images/img.sprites/{i}.tres");
        }

        _hideTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/hide.tres");
        _flagTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/flag.tres");
        _questionTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/question.tres");
        _mineTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/mine.tres");
        _explodeTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/explode.tres");
        _errorTexture =
            GD.Load<AtlasTexture>("res://resources/images/img.sprites/error.tres");
    }

    public void SetDisplay(CellState display, int adjacentMines) {
        _textureRect.Texture = display switch {
            CellState.Hidden => _hideTexture,
            CellState.Revealed when adjacentMines == -1 => _mineTexture,
            CellState.Revealed => GetNumberTexture(adjacentMines),
            CellState.Flagged => _flagTexture,
            CellState.Question => _questionTexture,
            _ => _hideTexture
        };
    }

    private static AtlasTexture? GetNumberTexture(int adjacentMines) {
        if (_numberTextures != null && adjacentMines is >= 0 and <= 8) {
            return _numberTextures[adjacentMines];
        }
        return _numberTextures?[0];
    }

    public void SetMineTriggered() {
        _textureRect.Texture = _explodeTexture;
    }

    public void SetWrongFlag() {
        _textureRect.Texture = _errorTexture;
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed) {
            var mask = mouseButton.ButtonMask;
            bool leftDown = (mask & MouseButtonMask.Left) != 0;
            bool rightDown = (mask & MouseButtonMask.Right) != 0;

            if (leftDown && rightDown) {
                EmitSignal(SignalName.ChordClicked);
                AcceptEvent();
            } else if (mouseButton.ButtonIndex == MouseButton.Left) {
                EmitSignal(SignalName.LeftClicked);
                AcceptEvent();
            } else if (mouseButton.ButtonIndex == MouseButton.Right) {
                EmitSignal(SignalName.RightClicked);
                AcceptEvent();
            } else if (mouseButton.ButtonIndex == MouseButton.Middle) {
                EmitSignal(SignalName.ChordClicked);
                AcceptEvent();
            }
        }
    }
}