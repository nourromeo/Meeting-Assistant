using XlentWPF.Models;
using XlentWPF.Services.Interfaces;

namespace XlentWPF.Services.Mock;


// Replace with OpenAiSummaryService when ready.

public class MockSummaryService : ISummaryService
{
    public async Task<SummaryResult> SummarizeAsync(
        IEnumerable<string> transcriptLines,
        CancellationToken cancellationToken = default)
    {
        // Simulate network latency
        await Task.Delay(1800, cancellationToken);

        return new SummaryResult
        {
            Overview = "Sprint review meeting covering authentication module deployment and " +
                       "dashboard redesign status. Team discussed timeline for production " +
                       "deployment and QA dependencies.",
            KeyPoints =
            [
                "Authentication module successfully deployed to staging environment",
                "Dashboard redesign completed, awaiting final stakeholder review",
                "QA security pass estimated at 2 more days",
                "Production deployment target: Friday end of week",
            ],
            ActionItems =
            [
                "QA team to complete security pass for auth module",
                "Alex to confirm Friday production deployment slot",
                "Maya to share dashboard design with stakeholders for approval",
            ]
        };
    }
}
