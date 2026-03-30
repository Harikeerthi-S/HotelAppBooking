import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Chatbot } from './chatbot';
import { APIService } from '../services/api.service';
import { of, throwError } from 'rxjs';
import { ChatResponseModel } from '../models/chat.model';
import { provideHttpClient } from '@angular/common/http';

describe('Chatbot', () => {
  let component: Chatbot;
  let fixture: ComponentFixture<Chatbot>;
  let apiSpy: jasmine.SpyObj<APIService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('APIService', ['apiChatMessage']);

    await TestBed.configureTestingModule({
      imports: [Chatbot],
      providers: [
        provideHttpClient(),
        { provide: APIService, useValue: apiSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Chatbot);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should start with greeting message', () => {
    expect(component.messages().length).toBe(1);
    expect(component.messages()[0].sender).toBe('bot');
  });

  it('should toggle open/close', () => {
    expect(component.isOpen()).toBeFalse();
    component.toggle();
    expect(component.isOpen()).toBeTrue();
    component.toggle();
    expect(component.isOpen()).toBeFalse();
  });

  it('should not send empty message', () => {
    component.inputText.set('   ');
    component.send();
    expect(apiSpy.apiChatMessage).not.toHaveBeenCalled();
  });

  it('should send message and receive bot reply', fakeAsync(() => {
    const mockReply: ChatResponseModel = {
      sessionId: 'test', reply: 'Hello!', intent: 'greeting', createdAt: new Date().toISOString()
    };
    apiSpy.apiChatMessage.and.returnValue(of(mockReply));

    component.inputText.set('Hello');
    component.send();
    tick();

    const msgs = component.messages();
    expect(msgs.some(m => m.sender === 'user' && m.message === 'Hello')).toBeTrue();
    expect(msgs.some(m => m.sender === 'bot' && m.message === 'Hello!')).toBeTrue();
    expect(component.sending()).toBeFalse();
  }));

  it('should show error message on API failure', fakeAsync(() => {
    apiSpy.apiChatMessage.and.returnValue(throwError(() => new Error('Network error')));

    component.inputText.set('test');
    component.send();
    tick();

    const msgs = component.messages();
    expect(msgs.some(m => m.sender === 'bot' && m.message.includes('could not connect'))).toBeTrue();
    expect(component.sending()).toBeFalse();
  }));

  it('should clear chat and add greeting', () => {
    component.clearChat();
    expect(component.messages().length).toBe(1);
    expect(component.messages()[0].sender).toBe('bot');
  });

  it('should send on Enter key', fakeAsync(() => {
    const mockReply: ChatResponseModel = {
      sessionId: 'test', reply: 'Hi!', intent: 'greeting', createdAt: new Date().toISOString()
    };
    apiSpy.apiChatMessage.and.returnValue(of(mockReply));

    component.inputText.set('Hi');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: false });
    component.onKeydown(event);
    tick();

    expect(apiSpy.apiChatMessage).toHaveBeenCalled();
  }));

  it('should NOT send on Shift+Enter', () => {
    component.inputText.set('Hi');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true });
    component.onKeydown(event);
    expect(apiSpy.apiChatMessage).not.toHaveBeenCalled();
  });

  it('should send quick reply', fakeAsync(() => {
    const mockReply: ChatResponseModel = {
      sessionId: 'test', reply: 'Booking info', intent: 'booking', createdAt: new Date().toISOString()
    };
    apiSpy.apiChatMessage.and.returnValue(of(mockReply));

    component.sendQuick('How to book?');
    tick();

    expect(apiSpy.apiChatMessage).toHaveBeenCalled();
  }));

  it('should format bold markdown', () => {
    const result = component.formatMessage('**Hello** world');
    expect(result).toContain('<strong>Hello</strong>');
  });

  it('should format newlines as <br>', () => {
    const result = component.formatMessage('line1\nline2');
    expect(result).toContain('<br>');
  });

  it('should expose quickReplies', () => {
    expect(component.quickReplies.length).toBeGreaterThan(0);
  });
});
