namespace Namespace {
    
    using re;
    
    using System.Collections.Generic;
    
    using System;
    
    using sys;
    
    public static class Module {
        
        //!/usr/bin/env python
        // -*- coding: utf-8 -*-
        // Read dependencies from file and strip off version numbers.
        // 
        //     Supports extra requirements and '>=' and '<=' version qualifiers.
        //     
        public static object parse_requirements(object requirements, object ignore = ValueTuple.Create("setuptools")) {
            using (var f = open(requirements)) {
                packages = new HashSet<object>();
                foreach (var line in f) {
                    line = line.strip();
                    if (!line || line.startswith(("#", "-r", "--"))) {
                        continue;
                    }
                    // XXX: Ugly hack!
                    extras = new List<object>();
                    line = re.sub(@"\s*\[(.*?)\]", get_extras, line);
                    if (line.Contains("#egg=")) {
                        line = line.split("#egg=")[1];
                    }
                    pkg = re.split("[=<>]=", line)[0].strip();
                    if (!ignore.Contains(pkg)) {
                        if (extras) {
                            pkg = String.Format("pkg [%s]", extras[0]);
                        }
                        packages.add(pkg);
                    }
                }
                return tuple(packages);
            }
            Func<object, object> get_extras = match => {
                extras.append(match.group(1));
            };
        }
    }
}
