using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class organizationagentmodulesetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "MemberShips");

            migrationBuilder.DropColumn(
                name: "OrganizationCode",
                table: "Agents");

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAgents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    AgentId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAgents_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    MembershipId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationRoles_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    StateId = table.Column<long>(type: "bigint", nullable: false),
                    LgaId = table.Column<long>(type: "bigint", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BusinessCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationLocations_BusinessOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "BusinessOrganizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationLocations_LGAs_LgaId",
                        column: x => x.LgaId,
                        principalTable: "LGAs",
                        principalColumn: "LgaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationLocations_LocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationLocations_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "StateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationAgentRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationAgentId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationAgentRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationAgentRoles_OrganizationAgents_OrganizationAgentId",
                        column: x => x.OrganizationAgentId,
                        principalTable: "OrganizationAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationAgentRoles_OrganizationRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "OrganizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAgentRoles_OrganizationAgentId",
                table: "OrganizationAgentRoles",
                column: "OrganizationAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAgentRoles_RoleId",
                table: "OrganizationAgentRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationAgents_OrganizationId",
                table: "OrganizationAgents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_LgaId",
                table: "OrganizationLocations",
                column: "LgaId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_LocationTypeId",
                table: "OrganizationLocations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_OrganizationId",
                table: "OrganizationLocations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLocations_StateId",
                table: "OrganizationLocations",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoles_OrganizationId",
                table: "OrganizationRoles",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationAgentRoles");

            migrationBuilder.DropTable(
                name: "OrganizationLocations");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships");

            migrationBuilder.DropTable(
                name: "OrganizationAgents");

            migrationBuilder.DropTable(
                name: "OrganizationRoles");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsPinEnabled",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "MemberShips",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationCode",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
