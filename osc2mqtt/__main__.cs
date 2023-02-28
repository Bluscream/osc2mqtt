namespace Namespace {
    
    using main = osc2mqtt.main;
    
    using sys;
    
    public static class Module {
        
        static Module() {
            sys.exit(main(sys.argv[1]) || 0);
        }
    }
}
