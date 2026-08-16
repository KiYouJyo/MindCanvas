namespace MindCanvas.Core.Commands;

public sealed class UndoRedoManager
{
    private readonly Stack<IUndoableCommand> _undo = new();
    private readonly Stack<IUndoableCommand> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoDescription => _undo.TryPeek(out var c) ? c.Description : null;
    public string? RedoDescription => _redo.TryPeek(out var c) ? c.Description : null;

    public void Execute(IUndoableCommand command)
    {
        command.Execute();
        _undo.Push(command);
        _redo.Clear();
    }

    public bool Undo()
    {
        if (!_undo.TryPop(out var command)) return false;
        command.Undo();
        _redo.Push(command);
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var command)) return false;
        command.Execute();
        _undo.Push(command);
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
