import { api, unwrap } from './common';

const US13 = {
  async updateProgramDates(programId, payload) {
    return unwrap(await api.put(`/hr/programs/${programId}/dates`, payload));
  },
};

export default US13;
