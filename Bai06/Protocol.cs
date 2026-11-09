namespace Bai06
{
    public static class Protocol
    {
        public const char Separator = '|';
        public const string All = "ALL";

        // Client -> Server
        public const string Join = "JOIN";
        public const string Msg = "MSG";
        public const string Pm = "PM";
        public const string FileMsg = "FILE";
        public const string Leave = "LEAVE";

        // Server -> Client
        public const string JoinOk = "JOINOK";
        public const string JoinFail = "JOINFAIL";
        public const string Sys = "SYS";
        public const string Users = "USERS";

        public static string Build(params string[] parts)
        {
            return string.Join(Separator.ToString(), parts);
        }

        public static string[] Split(string line, int maxParts)
        {
            return line.Split(new[] { Separator }, maxParts);
        }
    }
}