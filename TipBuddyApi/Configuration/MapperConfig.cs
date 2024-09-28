using AutoMapper;
using TipBuddyApi.Data;
using TipBuddyApi.Dtos.Shift;

namespace TipBuddyApi.Configuration
{
    public class MapperConfig : Profile
    {
        public MapperConfig()
        {
            CreateMap<CreateShiftDto, Shift>().ReverseMap();
            CreateMap<UpdateShiftDto, Shift>().ReverseMap();
            CreateMap<GetShiftDto, Shift>().ReverseMap();
        }
    }
}
