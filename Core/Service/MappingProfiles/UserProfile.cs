using AutoMapper;
using Domain.Entities;
using Service.PictureUrlResolver;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.MappingProfiles {
    public class UserProfile : Profile {
        public UserProfile() {
            CreateMap<ApplicationUser, UserProfileResultDTO>()
                .ForMember(dest => dest.ProfilePicture,
                    opt => opt.MapFrom<UserPictureUrlResolver>());
        }
    }
}
