namespace lotus2mqtt.LotusApi;

public class EcloudHttpException : Exception
{
    public EcloudResponse Response { get; init; }

    public EcloudHttpException(EcloudResponse response)
        :base(response.Message)
    {
        Response = response;
    }
}