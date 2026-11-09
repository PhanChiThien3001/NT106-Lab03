namespace Bai05
{
    /// <summary>
    /// Giao thuc tin nhan cua Bai 5 (moi tin la 1 dong, cac phan cach nhau boi '|').
    /// Client -> Server: JOIN|ten   ADD|tenmon   LIST|ME hoac ALL   RANDOM|ME hoac ALL   DEL|id
    /// Server -> Client: JOINOK|ten   OK|thongbao   ERR|loi
    ///                   LIST|scope|id:chusohuu:tenmon[TAB]id:chusohuu:tenmon...
    ///                   RANDOM|scope|chusohuu|tenmon
    /// </summary>
    public static class FoodProtocol
    {
        public const char Separator = '|';
        public const char ItemSeparator = '\t';

        public const string Me = "ME";
        public const string All = "ALL";

        // Client -> Server
        public const string Join = "JOIN";
        public const string Add = "ADD";
        public const string List = "LIST";
        public const string Random = "RANDOM";
        public const string Delete = "DEL";

        // Server -> Client
        public const string JoinOk = "JOINOK";
        public const string Ok = "OK";
        public const string Err = "ERR";

        public static string Build(params string[] parts)
        {
            return string.Join(Separator.ToString(), parts);
        }

        public static string[] Split(string line, int maxParts)
        {
            return line.Split(new[] { Separator }, maxParts);
        }

        // Loai bo ky tu dac biet de khong pha vo giao thuc
        public static string Clean(string text)
        {
            return text.Replace('|', ' ').Replace(':', ' ').Replace('\t', ' ')
                       .Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}

