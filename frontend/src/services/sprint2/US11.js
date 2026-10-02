import { api, unwrap } from './common';

const US11 = {
  async getDepartments() {
    return unwrap(await api.get('/hr/departments'));
  },

  async createDepartment(payload) {
    return unwrap(await api.post('/hr/departments', payload));
  },

  async getHrPrograms() {
    return unwrap(await api.get('/hr/programs'));
  },

  async createProgram(payload) {
    return unwrap(await api.post('/hr/programs', payload));
  },
};

export default US11;
