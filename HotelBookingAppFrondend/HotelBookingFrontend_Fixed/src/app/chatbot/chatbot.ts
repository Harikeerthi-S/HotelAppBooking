import { Component, inject, signal, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { APIService } from '../services/api.service';
import { ChatHistoryModel, ChatRequestModel } from '../models/chat.model';

@Component({
  selector: 'app-chatbot',
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.css'
})
export class Chatbot implements AfterViewChecked {
  @ViewChild('messagesEnd') private messagesEnd!: ElementRef;

  private api = inject(APIService);

  messages  = signal<ChatHistoryModel[]>([]);
  inputText = signal('');
  sending   = signal(false);
  isOpen    = signal(false);
  sessionId = `session-${Date.now()}`;

  private userId: number | null = null;

  constructor() {
    const raw = localStorage.getItem('userId');
    if (raw) this.userId = Number(raw);
    this.addBotMessage(
      "👋 Hi! I'm StayEase Assistant. Ask me about bookings, cancellations, hotels, or payments.",
      'greeting'
    );
  }

  ngAfterViewChecked(): void { this.scrollToBottom(); }

  toggle(): void { this.isOpen.update(v => !v); }

  send(): void {
    const text = this.inputText().trim();
    if (!text || this.sending()) return;

    this.addUserMessage(text);
    this.inputText.set('');
    this.sending.set(true);

    const req = new ChatRequestModel();
    req.sessionId = this.sessionId;
    req.message   = text;
    if (this.userId) req.userId = this.userId;

    this.api.apiChatMessage(req).subscribe({
      next: (res) => {
        this.addBotMessage(res.reply, res.intent);
        this.sending.set(false);
      },
      error: () => {
        this.addBotMessage('Sorry, I could not connect. Please try again.', 'error');
        this.sending.set(false);
      }
    });
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.send(); }
  }

  clearChat(): void {
    this.messages.set([]);
    this.addBotMessage("Chat cleared. How can I help you?", 'greeting');
  }

  private addUserMessage(text: string): void {
    this.messages.update(m => [...m, {
      chatMessageId: Date.now(), sender: 'user', message: text,
      sessionId: this.sessionId, createdAt: new Date().toISOString()
    }]);
  }

  private addBotMessage(text: string, intent: string): void {
    this.messages.update(m => [...m, {
      chatMessageId: Date.now() + 1, sender: 'bot', message: text,
      intent, sessionId: this.sessionId, createdAt: new Date().toISOString()
    }]);
  }

  private scrollToBottom(): void {
    try { this.messagesEnd?.nativeElement.scrollIntoView({ behavior: 'smooth' }); } catch {}
  }

  get quickReplies(): string[] {
    return ['How to book?', 'Cancellation policy', 'Payment methods', 'Hotel amenities'];
  }

  sendQuick(text: string): void { this.inputText.set(text); this.send(); }

  formatMessage(text: string): string {
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }
}
