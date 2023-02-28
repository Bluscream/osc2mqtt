namespace Namespace {
    
    using open = io.open;
    
    using setup = setuptools.setup;
    
    using parse_requirements = parse_requirements.parse_requirements;
    
    using DistributionMetadata = distutils.dist.DistributionMetadata;
    
    using System;
    
    using System.Linq;
    
    using System.Collections.Generic;
    
    public static class Module {
        
        static Module() {
            @"An OSC to MQTT bridge based on pyliblo and paho-mqtt.";
            DistributionMetadata.repository = null;
            setup(name: name, version: "0.2b2", description: @__doc__.splitlines()[0], long_description: "\n".join(readme.splitlines()[2]), keywords: "osc mqtt iot", classifiers: (from c in classifiers.splitlines()
                where c.strip() && !c.startswith("#")
                select c.strip()).ToList(), author: "Christopher Arndt", author_email: "chris@chrisarndt.de", url: url, repository: url, download_url: url + "/releases", license: "MIT License", platforms: "POSIX, Windows, MacOS X", packages: new List<object> {
                "osc2mqtt"
            }, install_requires: parse_requirements("requirements.txt"), entry_points: new Dictionary<object, object> {
                {
                    "console_scripts",
                    new List<object> {
                        "osc2mqtt = osc2mqtt.__main__:main"
                    }}}, zip_safe: true);
        }
        
        public static object classifiers = @"\
Development Status :: 4 - Beta
Environment :: Console
Intended Audience :: Developers
Intended Audience :: End Users/Desktop
Intended Audience :: Manufacturing
Intended Audience :: Other Audience
License :: OSI Approved :: MIT License
Operating System :: Microsoft :: Windows
Operating System :: POSIX
Operating System :: MacOS :: MacOS X
Programming Language :: Python
Programming Language :: Python :: 2.7
Programming Language :: Python :: 3.3
Programming Language :: Python :: 3.4
Topic :: Communications
Topic :: Internet
Topic :: Home Automation
Topic :: Multimedia :: Sound/Audio
";
        
        public static object name = "osc2mqtt";
        
        public static object url = String.Format("https://github.com/SpotlightKid/%s", name.lower());
        
        public static object readme = open("README.rst", encoding: "utf-8").read();
    }
}
