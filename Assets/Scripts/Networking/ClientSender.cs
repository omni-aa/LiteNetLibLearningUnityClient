using System;
using System.Collections;
using LiteNetLib;
using MessagePack;
using SharedLibrary;
using UnityEngine;
using PacketSecurity = Shared.PacketSecurity;

public class ClientSender
{
    public void SendAuth(NetPeer peer, string token)
    {
        try
        {
            Debug.Log("=== SENDING AUTH PACKET ===");
            Debug.Log($"Auth Token: {token}");
            
            // Create auth packet
            var authPacket = new AuthPacket { Token = token };
            
            // Serialize with packet type
            byte[] packetData = PacketManager.Serialize(PacketType.Auth, authPacket);
            
            Debug.Log($"Raw packet data length: {packetData.Length}");
            Debug.Log($"Raw packet (hex): {BitConverter.ToString(packetData)}");
            Debug.Log($"First byte (packet type): {packetData[0]} = {(PacketType)packetData[0]}");
            
            // Show what AuthPacket serializes to
            try
            {
                string serializedJson = MessagePackSerializer.SerializeToJson(authPacket);
                Debug.Log($"AuthPacket as JSON: {serializedJson}");
            }
            catch { }
            
            // Use EncryptAndSign which combines HMAC + encryption
            byte[] finalPacket = PacketSecurity.EncryptAndSign(packetData);
            
            Debug.Log($"Final packet length: {finalPacket.Length}");
            Debug.Log($"HMAC part (first 8): {BitConverter.ToString(finalPacket, 0, 8)}...");
            Debug.Log($"Encrypted part (first 16): {BitConverter.ToString(finalPacket, 32, Math.Min(16, finalPacket.Length - 32))}...");
            
            // Verify we can decrypt our own packet
            TestSelfDecryption(finalPacket, packetData);
            
            // Send the packet
            peer.Send(finalPacket, DeliveryMethod.ReliableOrdered);
            
            NetworkLogger.Sent(PacketType.Auth);
            Debug.Log($"=== AUTH PACKET SENT ===\n");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending auth packet: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private void TestSelfDecryption(byte[] finalPacket, byte[] originalData)
    {
        try
        {
            Debug.Log("--- Testing self-decryption ---");
            
            // Extract HMAC and encrypted data
            byte[] hmac = new byte[32];
            byte[] encrypted = new byte[finalPacket.Length - 32];
            Array.Copy(finalPacket, 0, hmac, 0, 32);
            Array.Copy(finalPacket, 32, encrypted, 0, encrypted.Length);
            
            // Verify
            bool valid = PacketSecurity.Verify(encrypted, hmac);
            Debug.Log($"Self HMAC valid: {valid}");
            
            if (valid)
            {
                // Decrypt
                byte[] decrypted = PacketSecurity.Decrypt(encrypted);
                Debug.Log($"Self-decrypted length: {decrypted.Length}");
                
                // Compare with original
                bool matches = StructuralComparisons.StructuralEqualityComparer.Equals(originalData, decrypted);
                Debug.Log($"Self-decrypted matches original: {matches}");
                
                if (matches && decrypted.Length > 0)
                {
                    Debug.Log($"Self-decrypted first byte: {decrypted[0]} = {(PacketType)decrypted[0]}");
                }
            }
            Debug.Log("--- End self-decryption test ---");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Self-decryption test failed: {ex.Message}");
        }
    }
}