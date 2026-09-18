/**
 * Types mirroring the C# DTOs on the wire.
 * Kept in one file so any drift with the backend surfaces here first —
 * do NOT let the components define their own event / person shapes.
 */

export type EventRole =
  | 'victim'
  | 'suspect'
  | 'witness'
  | 'perpetrator'
  | 'reporter'
  | 'officer'

export interface EventPersonDto {
  personId: string
  firstName: string
  lastName: string
  role: EventRole
}

export interface EventLinkDto {
  fromEventId: string
  toEventId: string
  otherEventId: string
  otherEventTitle: string
  note?: string | null
  createdAt: string
}

export interface CrimeEventDto {
  id: string
  title: string
  description?: string | null
  address?: string | null
  occurredAt: string
  severity: number
  latitude?: number | null
  longitude?: number | null
  persons: EventPersonDto[]
  links: EventLinkDto[]
  createdAt: string
  updatedAt: string
}

export interface CrimeEventWriteDto {
  title: string
  description?: string | null
  address?: string | null
  occurredAt: string
  severity: number
  latitude?: number | null
  longitude?: number | null
}

export interface PersonDto {
  id: string
  firstName: string
  lastName: string
  citizenships: string[]
  passportNumbers: string[]
  phone?: string | null
  physicalDescription?: string | null
  createdAt: string
  updatedAt: string
}

export interface PersonWriteDto {
  firstName: string
  lastName: string
  citizenships: string[]
  passportNumbers: string[]
  phone?: string | null
  physicalDescription?: string | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CrimeEventListQuery {
  page?: number
  pageSize?: number
  from?: string
  to?: string
  minSeverity?: number
  maxSeverity?: number
}

export interface PersonListQuery {
  page?: number
  pageSize?: number
  q?: string
}
