namespace Namespace {
    
    using @absolute_import = @@__future__.absolute_import;
    
    using @unicode_literals = @@__future__.unicode_literals;
    
    using shlex;
    
    using System;
    
    using System.Linq;
    
    public static class Module {
        
        static Module() {
            @"Utility functions for reading osc2mqtt configuration.";
        }
        
        // -*- coding: utf-8 -*-
        public static object as_bool(object val) {
            return ("1", "enabled", "on", "t", "true", "y", "yes").Contains(val.ToString().lower());
        }
        
        public static object parse_hostport(object addr, object port = 9000) {
            object host;
            if (addr.Contains("::") && addr.Contains("]:") || !addr.Contains("::") && addr.Contains(":")) {
                var _tup_1 = addr.rsplit(":", 1);
                host = _tup_1.Item1;
                port = _tup_1.Item2;
            } else {
                host = addr;
            }
            if (host.startswith("[") && host.endswith("]")) {
                host = host[1:: - 1];
            }
            return (host, Convert.ToInt32(port));
        }
        
        public static object parse_list(object s) {
            var lexer = shlex.shlex(s, posix: true);
            lexer.whitespace = ",";
            lexer.whitespace_split = true;
            lexer.commenters = "";
            return (from token in lexer
                select token.strip()).ToList();
        }
    }
}
