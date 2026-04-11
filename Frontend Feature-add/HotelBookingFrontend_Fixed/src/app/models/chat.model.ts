export class ChatRequestModel {
  userId?: number;
  sessionId: string = '';
  message: string = '';
}

export class ChatResponseModel {
  sessionId: string = '';
  reply: string = '';
  intent: string = 'general';
  createdAt: string = '';
}

export class ChatHistoryModel {
  chatMessageId: number = 0;
  userId?: number;
  sessionId: string = '';
  sender: string = '';   // 'user' | 'bot'
  message: string = '';
  intent?: string;
  createdAt: string = '';
}
