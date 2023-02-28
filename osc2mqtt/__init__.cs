namespace Namespace {
    
    using @absolute_import = @@__future__.absolute_import;
    
    using @unicode_literals = @@__future__.unicode_literals;
    
    using argparse;
    
    using logging;
    
    using shlex;
    
    using sys;
    
    using time;
    
    using OrderedDict = collections.OrderedDict;
    
    using configparser;
    
    using configparser = ConfigParser;
    
    using liblo;
    
    using mqtt = paho.mqtt.client;
    
    using Osc2MqttConverter = converter.Osc2MqttConverter;
    
    using ConversionRule = converter.ConversionRule;
    
    using as_bool = util.as_bool;
    
    using parse_hostport = util.parse_hostport;
    
    using parse_list = util.parse_list;
    
    using System.Collections.Generic;
    
    using System;
    
    public static class Module {
        
        static Module() {
            @"Bridge between OSC and MQTT.";
            sys.exit(main(sys.argv[1]) || 0);
        }
        
        public static object log = logging.getLogger("osc2mqtt");
        
        public static object read_config(object filename, object options = "options") {
            var config = new Dictionary<object, object> {
                {
                    "rules",
                    OrderedDict()}};
            var defaults = new Dictionary<@string, object> {
                {
                    "match",
                    "^/?(.*)"},
                {
                    "address",
                    @"/\1"},
                {
                    "topic",
                    @"\1"},
                {
                    "address_groups",
                    null},
                {
                    "topic_groups",
                    null},
                {
                    "type",
                    "struct"},
                {
                    "format",
                    "B"},
                {
                    "from_mqtt",
                    null},
                {
                    "from_osc",
                    null},
                {
                    "osctags",
                    null}};
            if (filename) {
                var parser = configparser.RawConfigParser(defaults);
                parser.read(filename);
                if (parser.has_section(options)) {
                    var default_options = parser.items("DEFAULT");
                    config.update(from _tup_1 in parser.items(options).Chop((setting,value) => (setting, value))
                        let setting = _tup_1.Item1
                        let value = _tup_1.Item2
                        where !default_options.Contains(setting)
                        select (setting, value));
                }
                foreach (var section in parser.sections()) {
                    if (section.startswith(":")) {
                        var name = section[1];
                        config["rules"][name] = parser.items(section).ToDictionary();
                    }
                }
            }
            var subscriptions = parse_list(config.get("subscriptions", "#"));
            config["subscriptions"] = new List<object>();
            var encode = new byte[] {  } is str ? s => s.encode("utf-8") : s => s;
            foreach (var sub in subscriptions) {
                config["subscriptions"].append((encode(sub), 0));
            }
            return config;
        }
        
        public class Osc2MqttBridge
            : object {
            
            public Osc2MqttBridge(object config, object converter) {
                this.converter = converter;
                this.config = config;
                var _tup_1 = parse_hostport(config.get("mqtt_broker", "localhost"), 1883);
                this.mqtt_host = _tup_1.Item1;
                this.mqtt_port = _tup_1.Item2;
                this.osc_port = Convert.ToInt32(config.get("osc_port", 9001));
                this.osc_receiver = config.get("osc_receiver");
                this.subscriptions = config.get("subscriptions", new List<object> {
                    "#"
                });
                if (this.osc_receiver) {
                    var _tup_2 = parse_hostport(this.osc_receiver, 9000);
                    var host = _tup_2.Item1;
                    var port = _tup_2.Item2;
                    this.osc_receiver = liblo.Address(host, port, liblo.UDP);
                }
                this.mqttclient = mqtt.Client(config.get("client_id", "osc2mqtt"));
                this.mqttclient.on_connect = this.mqtt_connect;
                this.mqttclient.on_disconnect = this.mqtt_disconnect;
                this.mqttclient.on_message = this.handle_mqtt;
                this.oscserver = liblo.ServerThread(this.osc_port);
                this.oscserver.add_method(null, null, this.handle_osc);
            }
            
            // Start MQTT client and OSC listener.
            public virtual object start() {
                log.info("Connecting to MQTT broker %s:%s ...", this.mqtt_host, this.mqtt_port);
                this.mqttclient.connect(this.mqtt_host, this.mqtt_port);
                log.debug("Starting MQTT thread...");
                this.mqttclient.loop_start();
                log.info("Starting OSC server listening on port %s ...", this.osc_port);
                this.oscserver.start();
            }
            
            // Method docstring.
            public virtual object stop() {
                log.info("Stopping OSC server ...");
                this.oscserver.stop();
                log.debug("Stopping MQTT thread ...");
                this.mqttclient.loop_stop();
                log.info("Disconnecting from MQTT broker ...");
                this.mqttclient.disconnect();
            }
            
            public virtual object mqtt_connect(object client, object userdata, object flags, object rc) {
                log.debug("MQTT connect: %s", mqtt.connack_string(rc));
                if (rc == 0 && this.subscriptions) {
                    client.subscribe(this.subscriptions);
                }
            }
            
            public virtual object mqtt_disconnect(object client, object userdata, object rc) {
                log.debug("MQTT disconnect: %s", mqtt.error_string(rc));
            }
            
            public virtual object handle_mqtt(object client, object userdata, object msg) {
                log.debug("MQTT recv: %s %r", msg.topic, msg.payload);
                var res = this.converter.from_mqtt(msg.topic, msg.payload);
                if (res) {
                    if (this.osc_receiver) {
                        log.debug("OSC send: %s %r", res);
                        this.oscserver.send(this.osc_receiver, res[0], res[1]);
                    }
                } else {
                    log.debug("No rule match for MQTT topic '%s'.", msg.topic);
                }
            }
            
            public virtual object handle_osc(
                object oscaddr,
                object values,
                object tags,
                object clientaddr,
                object userdata) {
                log.debug("OSC recv: %s %r", oscaddr, values);
                var res = this.converter.from_osc(oscaddr, values, tags);
                if (res) {
                    log.debug("MQTT publish: %s %r", res);
                    this.mqttclient.publish(res);
                } else {
                    log.debug("No rule match for OSC address '%s'.", oscaddr);
                }
            }
        }
        
        public static object main(object args = null) {
            var ap = argparse.ArgumentParser(description: @__doc__.splitlines()[0]);
            ap.add_argument("-c", "--config", metavar: "FILENAME", @default: "osc2mqtt.ini", help: "Read configuration from given filename");
            ap.add_argument("-p", "--osc-port", type: @int, metavar: "PORT", @default: 9001, help: "Local OSC server (UDP) port (default: %(default)s)");
            ap.add_argument("-m", "--mqtt-broker", metavar: "ADDR[:PORT]", @default: "localhost:1883", help: "MQTT broker addr[:port] (default: %(default)s)");
            ap.add_argument("-o", "--osc-receiver", metavar: "ADDR[:PORT]", help: "Also bridge MQTT to OSC receiver addr[:port] via UDP (default: one-way)");
            ap.add_argument("-v", "--verbose", action: "store_true", help: "Enable verbose logging");
            args = ap.parse_args(args != null ? args : sys.argv[1]);
            var cfg = read_config(args.config);
            foreach (var opt in ("mqtt_broker", "osc_port", "osc_receiver", "verbose")) {
                var argval = getattr(args, opt);
                if (!cfg.Contains(opt) || argval != ap.get_default(opt)) {
                    cfg[opt] = argval;
                }
            }
            logging.basicConfig(level: as_bool(cfg["verbose"]) ? logging.DEBUG : logging.INFO, format: "%(levelname)s:%(message)s");
            var converter = Osc2MqttConverter(cfg["rules"]);
            var osc2mqtt = Osc2MqttBridge(cfg, converter);
            osc2mqtt.start();
            try {
                while (true) {
                    time.sleep(1);
                }
            } catch (KeyboardInterrupt) {
                log.info("Interrupted.");
            } finally {
                osc2mqtt.stop();
                log.info("Done.");
            }
        }
    }
}
