using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace hlasovanisvj.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    Roles = table.Column<string[]>(type: "text[]", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GlobalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Units = table.Column<List<string>>(type: "text[]", nullable: false),
                    Addresses = table.Column<List<string>>(type: "text[]", nullable: false),
                    IsPresent = table.Column<bool>(type: "boolean", nullable: false),
                    ShareValue = table.Column<double>(type: "double precision", nullable: false),
                    ShareFraction = table.Column<string>(type: "text", nullable: false),
                    IsGuest = table.Column<bool>(type: "boolean", nullable: false),
                    ProxyId = table.Column<int>(type: "integer", nullable: true),
                    SessionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Members_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CurrentSessionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PresentShare = table.Column<double>(type: "double precision", nullable: true),
                    InFavorShare = table.Column<double>(type: "double precision", nullable: true),
                    AgainstShare = table.Column<double>(type: "double precision", nullable: true),
                    AbstainedShare = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    OpenedResolutionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_Resolutions_OpenedResolutionId",
                        column: x => x.OpenedResolutionId,
                        principalTable: "Resolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    ResolutionId = table.Column<int>(type: "integer", nullable: false),
                    Choice = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votes_Resolutions_ResolutionId",
                        column: x => x.ResolutionId,
                        principalTable: "Resolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_GlobalId",
                table: "Members",
                column: "GlobalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_ProxyId",
                table: "Members",
                column: "ProxyId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_SessionId",
                table: "Members",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_CurrentSessionId",
                table: "Organizations",
                column: "CurrentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_UserId",
                table: "Organizations",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_SessionId",
                table: "Resolutions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OpenedResolutionId",
                table: "Sessions",
                column: "OpenedResolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OrganizationId",
                table: "Sessions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_MemberId",
                table: "Votes",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ResolutionId",
                table: "Votes",
                column: "ResolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Sessions_SessionId",
                table: "Members",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Sessions_CurrentSessionId",
                table: "Organizations",
                column: "CurrentSessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Resolutions_Sessions_SessionId",
                table: "Resolutions",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Sessions_CurrentSessionId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Resolutions_Sessions_SessionId",
                table: "Resolutions");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Resolutions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
