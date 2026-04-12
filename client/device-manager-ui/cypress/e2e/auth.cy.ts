/// <reference types="cypress" />

export {};

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
    sub: '22222222-2222-2222-2222-222222222222',
    name: 'Cypress Route User',
    email: 'route-user@example.com',
    role,
    exp: now + 3600,
  };

  return `${encodeBase64Url(JSON.stringify(header))}.${encodeBase64Url(JSON.stringify(payload))}.signature`;
};

describe('Auth route guards', () => {
  it('redirects non-admin user when opening /devices/new directly', () => {
    const token = createToken('User');

    cy.intercept('GET', '**/api/devices', {
      statusCode: 200,
      body: [],
    }).as('getDevices');

    cy.visit('/devices/new', {
      onBeforeLoad(win) {
        win.localStorage.setItem('device_manager_token', token);
      },
    });

    cy.url().should('include', '/devices');
    cy.wait('@getDevices');
    cy.contains('h1', 'Devices').should('be.visible');
    cy.contains('button', 'Add Device').should('not.exist');
  });

  it('allows authenticated user to generate AI description from details page', () => {
    const token = createToken('User');
    const deviceId = '00000000-0000-0000-0000-000000000123';

    cy.intercept('GET', `**/api/devices/${deviceId}`, {
      statusCode: 200,
      body: {
        id: deviceId,
        tag: 'TAG-DETAIL-001',
        name: 'Review Device',
        manufacturer: 'Acme',
        type: 0,
        operatingSystem: 'Android',
        osVersion: '14',
        processor: 'Tensor',
        ramAmount: '8GB',
        description: null,
        assignedUserId: null,
        assignedUser: null,
      },
    }).as('getDeviceById');

    cy.intercept('POST', `**/api/devices/${deviceId}/generate-description`, {
      statusCode: 200,
      body: 'Generated for user decision support.',
    }).as('generateDescriptionForDevice');

    cy.visit(`/devices/${deviceId}`, {
      onBeforeLoad(win) {
        win.localStorage.setItem('device_manager_token', token);
      },
    });

    cy.wait('@getDeviceById');
    cy.contains('button', 'Generate AI Description').click();
    cy.wait('@generateDescriptionForDevice').its('response.statusCode').should('eq', 200);
  });
});
