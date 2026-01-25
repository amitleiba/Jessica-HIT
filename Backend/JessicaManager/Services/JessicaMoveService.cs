using Grpc.Core;

namespace JessicaManager.Services
{
    public class JessicaMoveService : JessicaMoveController.JessicaMoveControllerBase
    {
        private readonly ILogger<JessicaMoveService> _logger;

        public JessicaMoveService(ILogger<JessicaMoveService> logger)
        {
            _logger = logger;
        }

        public override Task<MoveResponse> MoveJessica(MoveRequest request, ServerCallContext context)
        {
            return base.MoveJessica(request, context); //TODO: Move the jessica and return the right resoponse
        }
    }
}
