using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.PictureUrlResolver {
    public class UserPictureUrlResolver : IValueResolver<ApplicationUser, UserProfileResultDTO, string?> {
        private readonly IConfiguration _configuration;

        public UserPictureUrlResolver(IConfiguration configuration) {
            _configuration = configuration;
        }

        public string? Resolve(
            ApplicationUser source,
            UserProfileResultDTO destination,
            string? destMember,
            ResolutionContext context) {

            if (string.IsNullOrEmpty(source.ProfilePicture))
                return "No Profile Picture";

            var baseUrl = _configuration["ApiBaseUrl"];

            return $"{baseUrl}files/UsersImages/{source.ProfilePicture}";
        }
    }
}
