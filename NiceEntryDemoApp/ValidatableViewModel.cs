using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NiceEntryDemoApp;

public interface IValidatableViewModel
{
    ValidationErrors ValidationErrors { get; }
    bool IsValid { get; }
    void Validate();
}

public abstract class ValidatableViewModel : ObservableValidator, IValidatableViewModel
{
    protected ValidatableViewModel()
    {
        ValidationErrors = new ValidationErrors(this);
    }
    
    public ValidationErrors ValidationErrors { get; }
    public bool IsValid => ValidationErrors.IsValid;

    public void Validate()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(ValidationErrors));
        
    }
}

public class ValidationErrors
{
    private readonly ObservableValidator _observableValidator;

    public ValidationErrors(ObservableValidator observableValidator)
    {
        _observableValidator = observableValidator ?? throw new ArgumentNullException(nameof(observableValidator));
    }

    public bool IsValid => !_observableValidator.HasErrors;

    public IEnumerable<ValidationResult> Errors => _observableValidator.GetErrors();

    public ReadOnlyCollection<string> this[string propertyName] => _observableValidator.GetErrors(propertyName).Where(e => !string.IsNullOrEmpty(e.ErrorMessage)).Select(e => e.ErrorMessage!).ToList().AsReadOnly();
}
