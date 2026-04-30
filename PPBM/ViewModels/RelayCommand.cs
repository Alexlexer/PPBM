using System.Windows.Input;

namespace PPBM.ViewModels;

/// <summary>
/// An async-aware <see cref="ICommand"/> implementation that prevents re-entrant execution.
/// Delegates execution to an <c>async Task</c> callback and manages the enabled state
/// via <see cref="CommandManager.RequerySuggested"/>.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    /// <summary>
    /// Initializes a new instance of <see cref="RelayCommand"/>.
    /// </summary>
    /// <param name="execute">The async delegate to invoke when the command is executed.</param>
    /// <param name="canExecute">Optional delegate that determines whether the command can execute.</param>
    public RelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();
        try { await _execute(parameter); }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
