using System.Collections.Generic;

namespace ETG.Orleans.CodeGen.CodeGenParticipants
{
    public class CodeGenResult
    {
        private readonly IEnumerable<string> _usings;
        private readonly string _code;

        public IEnumerable<string> Usings
        {
            get { return _usings; }
        }

        public string Code {
            get { return _code;}
        }

        public CodeGenResult(string code, IEnumerable<string> usings)
        {
            _code = code;
            _usings = usings;
        }
    }
}