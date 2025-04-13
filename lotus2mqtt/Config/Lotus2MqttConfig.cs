using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotus2mqtt.Config
{
    public class LotusConfig
    {
        public LotusAccountConfig Account { get; set; } = new();

        public LotusMqttConfig Mqtt { get; set; } = new();
    }
}
