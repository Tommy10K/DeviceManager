type DeviceType = 0 | 1;

interface User {
  id: string;
  name: string;
  email: string;
  role: 0 | 1;
  location: string;
}

interface Device {
  id: string;
  tag: string;
  name: string;
  manufacturer: string;
  type: DeviceType;
  operatingSystem: string;
  osVersion: string;
  processor: string;
  ramAmount: string;
  description: string | null;
  assignedUserId: string | null;
  assignedUser: User | null;
}

const encodeBase64Url = (value: string): string =>
  Cypress.Buffer.from(value)
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');

const createToken = (role: 'Admin' | 'User'): string => {
  const now = Math.floor(Date.now() / 1000);
  const header = { alg: 'HS256', typ: 'JWT' };
  const payload = {
    sub: '11111111-1111-1111-1111-111111111111',
    name: 'Cypress User',
    email: 'cypress@example.com',
    role,
    exp: now + 3600,
  };

  return `${encodeBase64Url(JSON.stringify(header))}.${encodeBase64Url(JSON.stringify(payload))}.signature`;
};

describe('Devices CRUD flow', () => {
  let devices: Device[];
  let sequence = 2;

  const createDevice = (overrides: Partial<Device> = {}): Device => ({
    id: `00000000-0000-0000-0000-00000000000${sequence++}`,
    tag: 'TAG-001',
    name: 'Starter Phone',
    manufacturer: 'Acme',
    type: 0,
    operatingSystem: 'Android',
    osVersion: '14',
    processor: 'Snapdragon',
    ramAmount: '8GB',
    description: 'Seed device',
    assignedUserId: null,
    assignedUser: null,
    ...overrides,
  });

  beforeEach(() => {
    sequence = 2;
    devices = [
      createDevice({
        id: '00000000-0000-0000-0000-000000000001',
        tag: 'TAG-001',
      }),
    ];

    cy.intercept('GET', '**/api/devices', (req) => {
      req.reply({ statusCode: 200, body: devices });
    }).as('getDevices');

    cy.intercept('GET', '**/api/devices/*', (req) => {
      const id = req.url.split('/').pop() as string;
      const device = devices.find((item) => item.id === id);

      if (!device) {
        req.reply({ statusCode: 404 });
        return;
      }

      req.reply({ statusCode: 200, body: device });
    }).as('getDeviceById');

    cy.intercept('POST', '**/api/devices', (req) => {
      const newDevice = createDevice(req.body);
      devices.push(newDevice);
      req.reply({ statusCode: 201, body: newDevice });
    }).as('createDevice');

    cy.intercept('DELETE', '**/api/devices/*', (req) => {
      const id = req.url.split('/').pop() as string;
      devices = devices.filter((item) => item.id !== id);
      req.reply({ statusCode: 204, body: '' });
    }).as('deleteDevice');
  });

  it('renders list, creates device, deletes device, and opens details', () => {
    const newTag = 'TAG-NEW-001';
    const token = createToken('Admin');

    cy.visit('/devices', {
      onBeforeLoad(win) {
        win.localStorage.setItem('device_manager_token', token);
      },
    });
    cy.wait('@getDevices');

    cy.get('table.devices-table').should('be.visible');
    cy.contains('th', 'Tag').should('be.visible');

    cy.contains('button', 'Add Device').click();
    cy.url().should('include', '/devices/new');

    cy.get('input[formControlName="tag"]').type(newTag, { force: true });
    cy.get('input[formControlName="name"]').type('Cypress Phone', { force: true });
    cy.get('input[formControlName="manufacturer"]').type('Cypress Inc', { force: true });
    cy.get('input[formControlName="operatingSystem"]').type('Android', { force: true });
    cy.get('input[formControlName="osVersion"]').type('15', { force: true });
    cy.get('input[formControlName="processor"]').type('Tensor', { force: true });
    cy.get('input[formControlName="ramAmount"]').type('12GB', { force: true });
    cy.get('textarea[formControlName="description"]').type('Created during e2e test', { force: true });

    cy.contains('button', 'Create Device').click();
    cy.wait('@createDevice');
    cy.wait('@getDevices');

    cy.contains('td', newTag).should('be.visible');

    cy.contains('tr', newTag).within(() => {
      cy.contains('button', 'Delete').click();
    });

    cy.get('mat-dialog-container').should('be.visible');
    cy.get('mat-dialog-container').contains('button', 'Delete').click();

    cy.wait('@deleteDevice');
    cy.wait('@getDevices');

    cy.contains('td', newTag).should('not.exist');

    cy.contains('tr', 'TAG-001').within(() => {
      cy.contains('button', 'View').click();
    });

    cy.wait('@getDeviceById');
    cy.url().should('match', /\/devices\/[^/]+$/);
    cy.contains('h1', 'Starter Phone').should('be.visible');
    cy.contains('p', 'Tag: TAG-001').should('be.visible');
  });
});
