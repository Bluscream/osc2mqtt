namespace Namespace {
    
    using @absolute_import = @@__future__.absolute_import;
    
    using @unicode_literals = @@__future__.unicode_literals;
    
    using array;
    
    using json;
    
    using re;
    
    using @struct;
    
    using logging;
    
    using namedtuple = collections.namedtuple;
    
    using as_bool = util.as_bool;
    
    using parse_list = util.parse_list;
    
    using lru_cache = functools.lru_cache;
    
    using lru_cache = backports.functools_lru_cache.lru_cache;
    
    using lru_cache = lru_cache.lru_cache;
    
    using System.Collections.Generic;
    
    using System.Linq;
    
    using System;
    
    public static class Module {
        
        static Module() {
            @"Convert between MQTT message payload data and OSC arguments types.";
        }
        
        public static object log = logging.getLogger(@__name__);
        
        // Raised when configuration file can not be parsed correctly.
        public class ConfigError
            : Exception {
        }
        
        public static object ConversionRule = namedtuple("ConversionRule", new List<object> {
            "match",
            "address",
            "topic",
            "address_groups",
            "topic_groups",
            "type",
            "format",
            "from_mqtt",
            "from_osc",
            "osctags"
        });
        
        // Convert MQTT topic and payload to OSC address and values and vice-versa.
        //     
        public class Osc2MqttConverter
            : object {
            
            public object _converters = new Dictionary<object, object> {
                {
                    "f",
                    float},
                {
                    "float",
                    float},
                {
                    "i",
                    @int},
                {
                    "int",
                    @int},
                {
                    "s",
                    str},
                {
                    "str",
                    str},
                {
                    "b",
                    as_bool},
                {
                    "bool",
                    as_bool}};
            
            public Osc2MqttConverter(object rules) {
                this.rules = new Dictionary<object, object> {
                };
                foreach (var _tup_1 in rules.items()) {
                    var name = _tup_1.Item1;
                    var rule = _tup_1.Item2;
                    try {
                        if (rule["from_mqtt"] != null) {
                            rule["from_mqtt"] = (from f in parse_list(rule["from_mqtt"])
                                select this._converters.get(f)).ToList();
                        }
                        if (rule["from_osc"] != null) {
                            rule["from_osc"] = (from f in parse_list(rule["from_osc"])
                                select this._converters.get(f)).ToList();
                        }
                        if (rule["address_groups"] != null) {
                            rule["address_groups"] = parse_list(rule["address_groups"]);
                        }
                        if (rule["topic_groups"] != null) {
                            rule["topic_groups"] = parse_list(rule["topic_groups"]);
                        }
                        this.rules[name] = ConversionRule(rule);
                    } catch (Exception) {
                        throw ConfigError(String.Format("Malformed conversion rule: %s", exc));
                    }
                }
            }
            
            // Match MQTT topic or OSC address against a rule regex.
            // 
            //         @param topicoraddr: MQTT topic or OSC address pattern string
            //         @return ConversionRule: the conversion rule instance, which matched
            //             topicoraddr, or None, if no match was found
            // 
            //         
            [lru_cache]
            public virtual object match_rule(object topicoraddr) {
                foreach (var _tup_1 in this.rules.items()) {
                    var name = _tup_1.Item1;
                    var rule = _tup_1.Item2;
                    var match = re.search(rule.match, topicoraddr);
                    if (match) {
                        log.debug("Rule '%s' match on: %s", name, topicoraddr);
                        return Tuple.Create(rule, match);
                    }
                }
            }
            
            // Convert MQTT message to OSC.
            // 
            //         The MQTT message payload (an opaque byte string) can be encoded in
            //         several forms, for example:
            // 
            //         1. A JSON, msgpack, etc. string.
            //         2. An ASCII string representation of an integer or float
            //         3. An integer directly encoded as the byte value
            //            a. signed or
            //            b. unsigned
            //         4. A multi-byte value, like a word, long, float, double, etc. in signed,
            //            unsigned, big and little endian varieties
            //         5. An array (i.e. sequence) of values encoded as 3) or 4)
            // 
            //         1) and 2) can be decoded with JSON.loads(), 3a) can be passed to ord() and
            //         3a/b) and 4) can be decoded with struct.unpack(), where one needs to know
            //         the type, the endianess and signedness. 5) can be decoded, for example,
            //         with the array module or bytearray(), if the type of the values is uint8.
            // 
            //         
            public virtual object from_mqtt(object topic, object payload) {
                object values;
                var result = this.match_rule(topic);
                if (result) {
                    var _tup_1 = result;
                    var rule = _tup_1.Item1;
                    var match = _tup_1.Item2;
                    // add matches extracted from MQTTtopic to values
                    if (rule.topic_groups) {
                        var extra_values = match.group(rule.topic_groups);
                        if (rule.topic_groups.Count == 1) {
                            extra_values = new List<object> {
                                extra_values
                            };
                        } else {
                            extra_values = extra_values.ToList();
                        }
                        values = values + extra_values;
                    }
                    values = this.decode_values(payload, rule);
                    var addr_kwargs = match.groupdict("");
                    addr_kwargs["_values"] = values;
                    if (!(null, "").Contains(rule.from_mqtt)) {
                        values = this.convert_mqtt_values(rule.from_mqtt, values);
                    }
                    if (rule.osctags) {
                        values = tuple(zip(rule.osctags, values));
                    }
                    var addr = rule.address.format(match.groups(""), addr_kwargs);
                    log.debug("Using OSC address: %s", addr);
                    log.debug("Decoded payload to values: %r --> %r", payload, values);
                    return Tuple.Create(addr, values);
                }
            }
            
            // Convert decoded MQTT payload values via 'from_mqtt' conversion funcs.
            //         
            public virtual object convert_mqtt_values(object converters, object values) {
                return tuple(from _tup_1 in zip(converters, values).Chop((func,value) => (func, value))
                    let func = _tup_1.Item1
                    let value = _tup_1.Item2
                    select func ? func(value) : value);
            }
            
            // Decode MQTT message payload byte string into Python values.
            public virtual object decode_values(object data, object rule) {
                if (rule.type == "json") {
                    var values = json.loads(data.decode(rule.format || "utf-8"));
                } else if (rule.type == "struct") {
                    values = @struct.unpack_from(rule.format, data);
                } else if (rule.type == "array") {
                    values = tuple(array.array(rule.format, data));
                } else if (rule.type == "string") {
                    values = ValueTuple.Create(data.decode(rule.format || "utf-8"));
                } else {
                    values = ValueTuple.Create(data);
                }
                return values;
            }
            
            // Convert OSC message to MQTT.
            // 
            //         Since OSC messages always specify the types of their values, only the
            //         'type' and 'format' of the matching conversion rule is used to encode
            //         the OSC values into an MQTT message payload string.
            // 
            //         
            public virtual object from_osc(object addr, object values, object tags) {
                var result = this.match_rule(addr);
                if (result) {
                    var _tup_1 = result;
                    var rule = _tup_1.Item1;
                    var match = _tup_1.Item2;
                    // add matches extracted from OSC address to values
                    if (rule.address_groups) {
                        var extra_values = match.group(rule.address_groups);
                        if (rule.address_groups.Count == 1) {
                            extra_values = new List<object> {
                                extra_values
                            };
                        } else {
                            extra_values = extra_values.ToList();
                        }
                        values = values + extra_values;
                    }
                    var topic_kwargs = match.groupdict("");
                    topic_kwargs["_values"] = values;
                    if (!(null, "").Contains(rule.from_osc)) {
                        values = this.convert_osc_values(rule.from_osc, values);
                    }
                    var topic = rule.topic.format(match.groups(""), topic_kwargs);
                    log.debug("Using MQTT topic: %s", topic);
                    var data = this.encode_values(values, rule);
                    log.debug("Encoded values to payload: %r --> %r", values, data);
                    return Tuple.Create(topic, data);
                }
            }
            
            // Encode Python values into MQTT message payload.
            public virtual object encode_values(object values, object rule) {
                if (rule.type == "json") {
                    return json.dumps(values);
                } else if (rule.type == "struct") {
                    return bytearray(@struct.pack(rule.format, values));
                } else if (type == "array") {
                    return bytearray(array.array(rule.format, values).tostring());
                } else if (rule.type == "string") {
                    return "".join(from s in values
                        select s.ToString());
                } else if (values.Count == 1) {
                    return values[0].ToString().encode();
                } else {
                    return values.ToString().encode();
                }
            }
            
            // Convert values from OSC types via 'from_osc' conversion funcs.
            public virtual object convert_osc_values(object converters, object values) {
                return tuple(from _tup_1 in zip(converters, values).Chop((func,value) => (func, value))
                    let func = _tup_1.Item1
                    let value = _tup_1.Item2
                    select func ? func(value) : value);
            }
        }
    }
}
