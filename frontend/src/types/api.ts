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
  /** Row version; send it back when updating so concurrent edits are detected. */
  version: number
}

export interface CrimeEventWriteDto {
  title: string
  description?: string | null
  address?: string | null
  occurredAt: string
  severity: number
  latitude?: number | null
  longitude?: number | null
  version?: number
}

export interface PersonDto {
  id: string
  firstName: string
  lastName: string
  citizenships: string[]
  passportNumbers: string[]
  phones: string[]
  physicalDescription?: string | null
  createdAt: string
  updatedAt: string
  version: number
}

export interface PersonWriteDto {
  firstName: string
  lastName: string
  citizenships: string[]
  passportNumbers: string[]
  phones: string[]
  physicalDescription?: string | null
  version?: number
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

export type UserRole = 'Admin' | 'Analyst' | 'Viewer'

export const USER_ROLES: UserRole[] = ['Admin', 'Analyst', 'Viewer']

export interface CurrentUserDto {
  id: string
  email: string
  displayName: string
  role: UserRole
}

export interface UserDto extends CurrentUserDto {
  isLocked: boolean
  createdAt: string
}

export interface CreateUserRequest {
  email: string
  displayName: string
  role: UserRole
  password: string
}

export interface AuditEntryDto {
  id: number
  occurredAt: string
  userId?: string | null
  userName: string
  action: 'Created' | 'Updated' | 'Deleted'
  entityType: string
  entityId: string
  /** JSON object: property -> { old, new } */
  changes: string
}

export interface AuditQuery {
  entityType?: string
  entityId?: string
  userId?: string
  page?: number
  pageSize?: number
}
