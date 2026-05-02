using Microsoft.AspNetCore.Mvc;
using WaveApiMimiker.Data;

namespace WaveApiMimiker.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    /// <summary>
    /// Returns the list of pre-seeded sample accounts.
    /// All accounts share PIN 1234. Use /api/auth/login to get a JWT token.
    /// </summary>
    [HttpGet("sample")]
    public IActionResult SampleAccounts()
    {
        var accounts = new[]
        {
            // ── Senegal ──────────────────────────────────────────────────────
            new { phone = "+221771000001", name = "Amadou Diallo",     country = "SN", currency = "XOF", balance = 250_000, role = "Customer", pin = "1234" },
            new { phone = "+221772000002", name = "Fatou Ndiaye",      country = "SN", currency = "XOF", balance = 80_000,  role = "Customer", pin = "1234" },
            new { phone = "+221773000003", name = "Moussa Sow",        country = "SN", currency = "XOF", balance = 500_000, role = "Agent",    pin = "1234" },
            new { phone = "+221774000004", name = "Aïssatou Ba",       country = "SN", currency = "XOF", balance = 45_000,  role = "Customer", pin = "1234" },
            new { phone = "+221775000005", name = "Ibrahima Fall",     country = "SN", currency = "XOF", balance = 1_200_000, role = "Customer", pin = "1234" },
            new { phone = "+221778689865", name = "Ousmane Diaby",     country = "SN", currency = "XOF", balance = 100_000, role = "Customer", pin = "1234" },
            new { phone = "+221773005100", name = "Aliou Mbaye",       country = "SN", currency = "XOF", balance = 100_000, role = "Customer", pin = "1234" },
            new { phone = "+2207878788",   name = "Cheikh Sarr",       country = "SN", currency = "XOF", balance = 100_000, role = "Customer", pin = "1234" },

            // ── Côte d'Ivoire ────────────────────────────────────────────────
            new { phone = "+2250701000001", name = "Koffi Assi",       country = "CI", currency = "XOF", balance = 300_000, role = "Customer", pin = "1234" },
            new { phone = "+2250702000002", name = "Aya Touré",        country = "CI", currency = "XOF", balance = 150_000, role = "Customer", pin = "1234" },
            new { phone = "+2250703000003", name = "Mamadou Koné",     country = "CI", currency = "XOF", balance = 700_000, role = "Agent",    pin = "1234" },

            // ── Mali ─────────────────────────────────────────────────────────
            new { phone = "+22370000001",   name = "Seydou Keïta",     country = "ML", currency = "XOF", balance = 175_000, role = "Customer", pin = "1234" },
            new { phone = "+22371000002",   name = "Mariam Coulibaly", country = "ML", currency = "XOF", balance = 90_000,  role = "Customer", pin = "1234" },
            new { phone = "+22372000003",   name = "Boubacar Traoré",  country = "ML", currency = "XOF", balance = 400_000, role = "Agent",    pin = "1234" },

            // ── Burkina Faso ─────────────────────────────────────────────────
            new { phone = "+22670000001",   name = "Rasmané Ouédraogo",country = "BF", currency = "XOF", balance = 60_000,  role = "Customer", pin = "1234" },
            new { phone = "+22671000002",   name = "Adja Sawadogo",    country = "BF", currency = "XOF", balance = 220_000, role = "Customer", pin = "1234" },

            // ── Uganda ───────────────────────────────────────────────────────
            new { phone = "+256701000001",  name = "Moses Okonkwo",    country = "UG", currency = "UGX", balance = 800_000,   role = "Customer", pin = "1234" },
            new { phone = "+256702000002",  name = "Grace Nakato",     country = "UG", currency = "UGX", balance = 250_000,   role = "Customer", pin = "1234" },
            new { phone = "+256703000003",  name = "John Ssekibuule",  country = "UG", currency = "UGX", balance = 2_000_000, role = "Agent",    pin = "1234" },

            // ── Gambia ───────────────────────────────────────────────────────
            new { phone = "+2203000001",    name = "Lamin Jallow",     country = "GM", currency = "GMD", balance = 12_000, role = "Customer", pin = "1234" },
            new { phone = "+2203000002",    name = "Sainabou Ceesay",  country = "GM", currency = "GMD", balance = 5_500,  role = "Customer", pin = "1234" },
            new { phone = "+220000009",     name = "Binta Jammeh",     country = "GM", currency = "GMD", balance = 8_000,  role = "Customer", pin = "1234" },

            // ── Ghana ────────────────────────────────────────────────────────
            new { phone = "+233501000001",  name = "Kwame Mensah",     country = "GH", currency = "GHS", balance = 3_500, role = "Customer", pin = "1234" },
            new { phone = "+233502000002",  name = "Akua Boateng",     country = "GH", currency = "GHS", balance = 1_200, role = "Customer", pin = "1234" },
            new { phone = "+233503000003",  name = "Kofi Agyemang",    country = "GH", currency = "GHS", balance = 8_000, role = "Agent",    pin = "1234" },
        };

        var countries = WaveConstants.PhonePrefixes
            .Where(p => WaveConstants.SupportedCountries.Contains(p.CountryCode))
            .Select(p => new { p.Prefix, p.CountryCode, currency = WaveConstants.CountryCurrencies[p.CountryCode] })
            .DistinctBy(p => p.CountryCode)
            .OrderBy(p => p.CountryCode)
            .ToList();

        return Ok(new
        {
            note = "All accounts use PIN '1234'. POST to /api/auth/login to receive a JWT token. CountryCode is auto-detected from the phone prefix when registering.",
            totalAccounts = accounts.Length,
            supportedCountries = countries,
            accounts
        });
    }
}
