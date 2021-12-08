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
                .ForMember(dest => dest.LoadBalancerIp, opt =>
                    opt.MapFrom(src => src.LoadBalancer == null ? null : src.LoadBalancer.PublicIp));
        }
    }
}