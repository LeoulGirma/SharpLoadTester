using System.CommandLine;
using System.CommandLine.Binding;
using SharpLoadTester;
public class OptionsBinder : BinderBase<Options>
{
    private readonly Option<string> _urlOption;
    private readonly Option<int?> _rpsOption;
    private readonly Option<int> _totalRequestsOption;
    private readonly Option<int> _concurrencyOption;
    private readonly Option<int?> _startIdOption;
    private readonly Option<int?> _endIdOption;
    private readonly Option<string> _methodOption;
    private readonly Option<string?> _headersOption;
    private readonly Option<string?> _bodyOption;
    private readonly Option<string?> _outputOption;
    private readonly Option<int?> _thresholdOption;
    private readonly Option<string> _logLevelOption;
    public OptionsBinder(
        Option<string> urlOption,
        Option<int?> rpsOption,
        Option<int> totalRequestsOption,
        Option<int> concurrencyOption,
        Option<int?> startIdOption,
        Option<int?> endIdOption,
        Option<string> methodOption,
        Option<string?> headersOption,
        Option<string?> bodyOption,
        Option<string?> outputOption,
        Option<int?> thresholdOption,
        Option<string> logLevelOption)
    {
        _urlOption = urlOption;
        _rpsOption = rpsOption;
        _totalRequestsOption = totalRequestsOption;
        _concurrencyOption = concurrencyOption;
        _startIdOption = startIdOption;
        _endIdOption = endIdOption;
        _methodOption = methodOption;
        _headersOption = headersOption;
        _bodyOption = bodyOption;
        _outputOption = outputOption;
        _thresholdOption = thresholdOption;
        _logLevelOption = logLevelOption;
    }
    protected override Options GetBoundValue(BindingContext bindingContext)
    {
        return new Options
        {
            Url = bindingContext.ParseResult.GetValueForOption(_urlOption),
            RequestsPerSecond = bindingContext.ParseResult.GetValueForOption(_rpsOption),
            TotalRequests = bindingContext.ParseResult.GetValueForOption(_totalRequestsOption),
            Concurrency = bindingContext.ParseResult.GetValueForOption(_concurrencyOption),
            StartId = bindingContext.ParseResult.GetValueForOption(_startIdOption),
            EndId = bindingContext.ParseResult.GetValueForOption(_endIdOption),
            Method = bindingContext.ParseResult.GetValueForOption(_methodOption),
            Headers = bindingContext.ParseResult.GetValueForOption(_headersOption),
            Body = bindingContext.ParseResult.GetValueForOption(_bodyOption),
            Output = bindingContext.ParseResult.GetValueForOption(_outputOption),
            Threshold = bindingContext.ParseResult.GetValueForOption(_thresholdOption),
            LogLevel = bindingContext.ParseResult.GetValueForOption(_logLevelOption)
        };
    }
}
