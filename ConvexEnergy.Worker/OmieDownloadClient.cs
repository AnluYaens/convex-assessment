using Microsoft.Extensions.Options;

namespace ConvexEnergy.Worker;

public sealed class OmieDownloadClient(
    HttpClient http,
    IOptions<OmieOptions> options,
    ILogger<OmieDownloadClient> logger)
{
    /// Probes file versions 1..MaxVersionProbe and returns the highest one that
    /// exists and looks like a valid marginalpdbc file, or null if none exist.
    /// Corrections are republished as .2/.3, and some days only exist as .2,
    /// so we cannot stop at the first miss until we have found at least one.
    public async Task<(int Version, string Content)?> TryDownloadLatestAsync(
        DateOnly deliveryDate, CancellationToken ct)
    {
        var opts = options.Value;
        (int Version, string Content)? best = null;

        for (var version = 1; version <= opts.MaxVersionProbe; version++)
        {
            var url = string.Format(opts.DownloadUrlTemplate,
                deliveryDate.ToString("yyyyMMdd"), version);

            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                if (best is not null) break; // had .N, .N+1 missing: done
                continue;                    // .1 can be missing while .2 exists
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            if (!content.StartsWith("MARGINALPDBC"))
            {
                // OMIE returns 200 with junk/empty bodies for some nonexistent files
                logger.LogDebug("Version {Version} for {Day} returned non-marginalpdbc content, ignoring",
                    version, deliveryDate);
                if (best is not null) break;
                continue;
            }

            best = (version, content);
        }

        return best;
    }
}