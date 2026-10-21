import { adminRepository } from '../../infrastructure/repositories/adminRepository.js'

export const getAdminStatistics = () => adminRepository.statistics()
export const getRouletteAdminSettings = () => adminRepository.rouletteSettings()
export const saveRouletteAdminSettings = settings => adminRepository.updateRouletteSettings(settings)
export const getAdminUsers = () => adminRepository.admins()
export const createAdmin = details => adminRepository.createAdmin(details)
export const getAdminAuditLog = () => adminRepository.auditLog()
