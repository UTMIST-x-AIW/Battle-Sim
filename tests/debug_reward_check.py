class Creature:
    def __init__(self):
        self.maxHealthDefault = 3.0
        self.attackCooldownDefault = 1.0
        self.attackDamageDefault = 1.0
        self.moveSpeedDefault = 5.0
        self.rockHealthBonus = 15.0
        self.treeCooldownBonus = 0.05
        self.enemyDamageBonus = 2.0
        self.cupcakeSpeedBonus = 0.3
        self.maxHealthCap = 10.0
        self.minAttackCooldown = 0.1
        self.attackDamageCap = 5.0
        self.moveSpeedCap = 6.0
        self.maxHealth = self.maxHealthDefault
        self.attackCooldown = self.attackCooldownDefault
        self.attackDamage = self.attackDamageDefault
        self.moveSpeed = self.moveSpeedDefault

    def ModifyMaxHealth(self):
        self.maxHealth = min(self.maxHealth + self.rockHealthBonus, self.maxHealthCap)

    def ModifyAttackCooldown(self):
        self.attackCooldown = max(self.attackCooldown - self.treeCooldownBonus, self.minAttackCooldown)

    def ModifyAttackDamage(self):
        self.attackDamage = min(self.attackDamage + self.enemyDamageBonus, self.attackDamageCap)

    def ModifyMoveSpeed(self):
        self.moveSpeed = min(self.moveSpeed + self.cupcakeSpeedBonus, self.moveSpeedCap)

class Rock:
    def OnDestroyed(self, c):
        c.ModifyMaxHealth()

class Tree:
    def OnDestroyed(self, c):
        c.ModifyAttackCooldown()

class Enemy:
    def OnDestroyed(self, c):
        c.ModifyAttackDamage()

class Cupcake:
    def OnDestroyed(self, c):
        c.ModifyMoveSpeed()

c = Creature()
print('Initial', c.maxHealth, c.attackCooldown, c.attackDamage, c.moveSpeed)
Rock().OnDestroyed(c)
print('After rock', c.maxHealth)
Tree().OnDestroyed(c)
print('After tree', c.attackCooldown)
Enemy().OnDestroyed(c)
print('After enemy', c.attackDamage)
Cupcake().OnDestroyed(c)
print('After cupcake', c.moveSpeed)
