namespace datasheetapi.Controllers;

[ApiController]
[Route("models")]
public class ModelsController : ControllerBase
{
    [HttpGet("/instrument", Name = "GetInstrumentTagDataModel")]
    public ActionResult<InstrumentTagDataDto> GetInstrumentTagDataModel()
    {
        return new InstrumentTagDataDto();
    }

    [HttpGet("/electrical", Name = "GetElectricalTagDataModel")]
    public ActionResult<ElectricalTagDataDto> GetElectricalTagDataModel()
    {
        return new ElectricalTagDataDto();
    }

    [HttpGet("/mechanical", Name = "GetMechanicalTagDataModel")]
    public ActionResult<MechanicalTagDataDto> GetMechanicalTagDataModel()
    {
        return new MechanicalTagDataDto();
    }
}
