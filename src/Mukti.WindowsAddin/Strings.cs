namespace Mukti.WindowsAddin;

// Bilingual strings — English and Bangla
// Used for any strings that need to be computed at runtime, not just in XAML
internal static class Strings
{
    internal static string Get(bool english, string key) => key switch
    {
        "scanning"     => english ? "Scanning document..." : "নথি স্ক্যান করা হচ্ছে...",
        "no_bijoy"     => english ? "No Bijoy/SutonnyMJ text found." : "কোনো বিজয় লেখা পাওয়া যায়নি।",
        "found_runs"   => english ? "Found {0} run(s) to convert." : "{0}টি রান পাওয়া গেছে।",
        "applying"     => english ? "Applying conversion..." : "রূপান্তর প্রয়োগ হচ্ছে...",
        "done"         => english ? "Done — {0} run(s) converted." : "সম্পন্ন — {0}টি রান।",
        "reverting"    => english ? "Reverting..." : "ফিরিয়ে আনা হচ্ছে...",
        "reverted"     => english ? "Reverted successfully." : "পূর্বাবস্থায় ফেরানো হয়েছে।",
        "no_doc"       => english ? "No document open." : "কোনো নথি খোলা নেই।",
        _              => key
    };
}
