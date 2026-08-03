global using AnbuFight.Application.Common.Exceptions;
global using AnbuFight.Application.Common.Extensions;
global using AnbuFight.Application.Common.Interfaces;
global using AnbuFight.Application.Common.Models;
global using AnbuFight.Application.Common.Options;
global using AnbuFight.Application.Common.Validation;
global using AnbuFight.Domain.Entities;
global using AnbuFight.Domain.Enums;
global using FluentValidation;
global using MediatR;
global using Microsoft.EntityFrameworkCore;

// FluentValidation ships its own ValidationException; the application one always wins.
global using ValidationException = AnbuFight.Application.Common.Exceptions.ValidationException;
