using WiseTorrent.Trackers.Interfaces;

namespace WiseTorrent.Trackers.Classes
{
    internal class HTTPTrackerClient : ITrackerClient
    {
	    private readonly HttpClient _httpClient;

	    public HTTPTrackerClient()
	    {
		    _httpClient = new HttpClient();
	    }

	    public void InitialiseClient(string baseUri)
	    {
		    _httpClient.BaseAddress = new Uri(baseUri, UriKind.Absolute);
	    }
    }
}
