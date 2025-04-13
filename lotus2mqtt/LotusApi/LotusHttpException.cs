namespace lotus2mqtt.LotusApi;

public class LotusHttpException : Exception
{
    public LotuscarsResponse Response { get; init; }

    public LotusHttpException(LotuscarsResponse response)
        :base(response.Message)
    {
        Response = response;
    }
}