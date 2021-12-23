using AutoMapper;
using TG.Manager.Service.Entities;
using TG.Manager.Service.Models.Response;

namespace TG.Manager.Service.Config.Mapper
{
    public class BattleServerProfile : Profile
    {
        public BattleServerProfile()
        {
            CreateMap<BattleServer, BattleServerResponse>()
                .ForMember(dest => dest.Ip, opt =>
                    opt.MapFrom(src => src.NodePort == null ? null : src.NodeIp));
            CreateMap<TestBattleServer, BattleServerResponse>();
        }
    }
}