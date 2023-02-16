# WakeMyTv
WOL Tv when PC wake up
When you are using TV as you PC monitor you will notice that your PC can't wake up TV once it switch off, this is caused by the lack of CEC.
This program will send wake on lan packet to your smart tv connected to same network when you wake your pc by moving mouse or pressing a keys.
You will have to edit the MAC address and IP in the code to make it work for ya ;)

  // Set the IP and MAC address of your TV
        public String TvIpAddress = "192.168.0.28";
        public String TvMacAddress = "64:CB:E9:8D:E8:7E";
