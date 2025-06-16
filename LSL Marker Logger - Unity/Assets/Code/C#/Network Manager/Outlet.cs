using LSL;

namespace MINE
{
    public class Outlet
    {
        #region [ Unserialised Fields ]
        private StreamOutlet _outletStream;
        #endregion
        
        public void Initialise()
        {
            StreamInfo stream = new StreamInfo("Unity Output", "Markers", 1, LSL.LSL.IRREGULAR_RATE,
                channel_format_t.cf_string);
            _outletStream = new StreamOutlet(stream);
        }

        public void PushSample(string content)
        {
            string[] output = { content };
            _outletStream.push_sample(output);
        }
    }
}